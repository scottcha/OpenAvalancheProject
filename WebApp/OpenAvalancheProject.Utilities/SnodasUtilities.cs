using OSGeo.GDAL;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenAvalancheProject.Utilities
{
    public static class SnodasUtilities
    {
        static SnodasUtilities()
        {
            GdalConfiguration.ConfigureGdal();
        }

        public static List<string> UnpackSnodasArchive(string snodasFileToUncompress)
        { 
            using (Stream stream = File.OpenRead(snodasFileToUncompress))
            {
                return UnpackSnodasStream(stream);
            }
        }

        public static List<string> UnpackSnodasStream(Stream snodasStream)
        {
            var files = new List<string>();
            var uncompressedFiles = new List<string>();
            using (var reader = ReaderFactory.Open(snodasStream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        string filePath = Path.Combine(Path.GetTempPath(), reader.Entry.Key);
                        files.Add(filePath);
                        
                        reader.WriteEntryToDirectory(Path.GetTempPath(), new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true,
                        });
                    }
                }
            }
            foreach (var file in files)
            {
                using (Stream stream = File.OpenRead(file))
                using (var reader = ReaderFactory.Open(stream))
                {
                    while (reader.MoveToNextEntry())
                    {
                        string filePath = Path.Combine(Path.GetTempPath(), reader.Entry.Key);
                        uncompressedFiles.Add(filePath);
                        reader.WriteEntryToDirectory(Path.GetTempPath(), new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true,
                        });
                    }
                }
            }
            return uncompressedFiles;
        }

        /// <summary>
        /// Snodas OGR HDR files have too many bard codes for OGR to be able to read them (I have no idea why)
        /// We need to truncate these.
        /// </summary>
        /// <param name="filePath">Path to the Hdr file</param>
        /// <param name="outFilePath">Optional out file path; if not specified original file will be overwritten</param>
        public static void RemoveBardCodesFromHdr(string filePath, string outFilePath = null)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".hdr")
            {
                throw new ArgumentException($"filePath parameter has invalid extension as {extension} but expected .hdr");
            }

            string outContents = "";

            using (Stream stream = File.Open(filePath, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                var contents = reader.ReadToEnd();
                string pattern = @"[0-9]+ BARD codes:[0-9 ]+";
                Regex regex = new Regex(pattern);
                outContents = regex.Replace(contents, "0");
            }

            if(outFilePath == null)
            {
                outFilePath = filePath;
            }

            using (Stream stream = File.Open(outFilePath, FileMode.OpenOrCreate))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(outContents);
            }
        }

        public static List<SnodasRow> GetValuesForCoordinates(List<(double Lat, double Lon)> coordinates,
                                                              List<string> filePaths)
        {
            var results = new Dictionary<(double Lat, double Lon), SnodasRow>();
            foreach(var file in filePaths)
            {
                SnowdasListForCoordinates(coordinates, results, file);
            }
            return results.Values.ToList();
        }

        public static void SnowdasListForCoordinates(List<(double Lat,double Lon)> coordinates,
                                                     Dictionary<(double Lat, double Lon), SnodasRow> results,
                                                     string filePath)
        {
            if(!filePath.EndsWith(".Hdr"))
            {
                throw new ArgumentException($"filePaths must end with .Hdr extension for file: {filePath}");
            }
            using (var dataSet = Gdal.Open(filePath, Access.GA_ReadOnly))
            {
                double[] geoTransform = new double[6];
                double[] invGeoTransform = new double[6];
                dataSet.GetGeoTransform(geoTransform);
                Gdal.InvGeoTransform(geoTransform, invGeoTransform);
                var rasterBand = dataSet.GetRasterBand(1);
                var width = rasterBand.XSize;
                var height = rasterBand.YSize;
                double[] rasterArray = new double[width * height];
                DateTime date = DateTime.Parse(dataSet.GetMetadataItem("Stop_Date", null));
                var error = rasterBand.ReadRaster(0, 0, width, height, rasterArray, width, height, 0, 0);
                if (error != CPLErr.CE_None)
                {
                    throw new Exception($"Error reading raster {error}");
                }
                foreach(var coordinate in coordinates)
                {
                    SnodasRow row;
                    if (results.ContainsKey(coordinate))
                    {
                        row = results[coordinate];
                    }
                    else
                    {
                        row = new SnodasRow(coordinate.Lat, coordinate.Lon);
                    }
                    Gdal.ApplyGeoTransform(invGeoTransform, coordinate.Lon, coordinate.Lat, out double xoff, out double yoff);
                    var value = rasterArray[(int)xoff + (int)yoff * width];
                    row.Date = date;
                    //which variable are we getting from this file
                    if (filePath.Contains("1036"))
                    {
                        row.SNOWDAS_SnowDepth_mm = value;
                    }
                    else if (filePath.Contains("1034"))
                    {
                        row.SNOWDAS_SWE_mm = value;
                    }
                    else if (filePath.Contains("1044"))
                    {
                        row.SNOWDAS_SnowmeltRunoff_micromm = value;
                    }
                    else if (filePath.Contains("1050"))
                    {
                        row.SNOWDAS_Sublimation_micromm = value;
                    }
                    else if (filePath.Contains("1039"))
                    {
                        row.SNOWDAS_SublimationBlowing_micromm = value;
                    }
                    else if (filePath.Contains("1025SlL01"))
                    {
                        row.SNOWDAS_SolidPrecip_kgpersquarem = value;
                    }
                    else if (filePath.Contains("1025SlL00"))
                    {
                        row.SNOWDAS_LiquidPrecip_kgpersquarem = value;
                    }
                    else if (filePath.Contains("1038"))
                    {
                        row.SNOWDAS_SnowpackAveTemp_k = value;
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown snodas parameter from file {filePath}");
                    }

                    if (results.ContainsKey(coordinate))
                    {
                        results[coordinate] = row;
                    }
                    else
                    {
                        results.Add(coordinate, row);
                    }
                }
            }
        }
    }
}
