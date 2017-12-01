using Grib.Api;
using Grib.Api.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenAvalancheProject.Pipeline
{
    /// <summary>
    /// General utilities to help us work with grib files and their encodings
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Decode the wind vectors from the grib model in to direction and speed as we want to treat these 
        /// seperately.
        /// </summary>
        /// <param name="UGRDValue"></param>
        /// <param name="VGRDValue"></param>
        /// <returns>(Direction, Speed)</returns>
        public static (double Direction, double Speed) DecodeWindVectors(double UGRDValue, double VGRDValue)
        {
            double Direction = 57.29578 * (Math.Atan2(UGRDValue, VGRDValue)) + 180.0;
            double Speed = Math.Sqrt(UGRDValue * UGRDValue + VGRDValue * VGRDValue);
            return (Direction, Speed);
        }

        /// <summary>
        /// From a grib file decode it in to an object representing a row in our output file
        /// </summary>
        /// <param name="file">GribFile</param>
        /// <returns>List of rows</returns>
        public static List<NamTableRow> ParseNamGribFile(GribFile file)
        {
            var Rows = new List<GribRow>();
            DateTime? refTime = null;
            foreach (GribMessage msg in file)
            {
                var tmpTime = msg.Time;
                //Total precip is a time step value so make sure the time we get is at the end of the period
                //hour 0 is always 0 because of this; but some forecasts have more than one hours between steps 
                //so make sure we enable this
                var stepSize = 0;
                if (msg.ParameterName == "Total precipitation")
                {
                    int endStep = 0;
                    int startStep = 0;
                    var success = Int32.TryParse(msg.StartStep, out startStep);
                    if (!success)
                    {
                        throw new Exception($"Unable to parse start step variable: {msg.StartStep} from grib file");
                    }

                    if(startStep != 0)
                    {
                        //some forecasts have sub intervals, ignore these
                        //if we start using forecasts other than the one produced at time 00 then we might need to modify this to account for different start steps
                        continue;
                    }

                    success = Int32.TryParse(msg.EndStep, out endStep);
                    if (!success)
                    {
                        throw new Exception($"Unable to parse end step variable: {msg.EndStep} from grib file");
                    }
                    tmpTime = tmpTime.AddHours(endStep);
                    stepSize = endStep - startStep;
                }

                foreach (var val in msg.GridCoordinateValues)
                {
                    if (val.IsMissing)
                    {
                        continue;
                    }
                    //Console.WriteLine("Lat: {0} Lon: {1} Name: {2} Time: {3} Level: {4} Val: {5}", val.Latitude, val.Longitude, msg.ParameterName, msg.Time, msg.Level, val.Value);
                    double lat = Math.Round(val.Latitude, 6, MidpointRounding.AwayFromZero);
                    double lon = Math.Round(-(360.0 - val.Longitude), 6, MidpointRounding.AwayFromZero);
                    refTime = msg.ReferenceTime;

                    Rows.Add(new GribRow
                    {
                        Lat = lat,
                        Lon = lon,
                        ParameterName = msg.ParameterName,
                        //ParameterShortName = msg.ParameterShortName, //accessing this is very slow; only add it if we need it
                        Time = tmpTime,
                        ReferenceTime = msg.ReferenceTime,
                        Level = msg.Level,
                        TypeOfLevel = msg.TypeOfLevel,
                        StepSize = stepSize,
                        Value = val.Value
                    });
                }
            }
            var rowLookup = Rows.ToLookup(r => new Tuple<double, double, DateTime>(r.Lat, r.Lon, r.Time));
            var namTable = new List<NamTableRow>();
            foreach (var key in rowLookup)
            {
                if(refTime == null)
                {
                    throw new InvalidDataException("refTime is null and should be a valid datetime");
                }
                var newRow = new NamTableRow(key.Key.Item3, refTime.Value, new Tuple<double, double>(key.Key.Item1, key.Key.Item2));

                foreach (var column in key)
                {
                    if (column.TypeOfLevel == "surface" || column.TypeOfLevel == "heightAboveGround")
                    {
                        if (column.Level == 0)
                        {
                            if (column.ParameterName == "Total precipitation")
                            {
                                newRow.APCPsurface = column.Value;
                                newRow.APCPStepSize = column.StepSize;
                                continue;
                            }
                            else if (column.ParameterName == "195")
                            {
                                newRow.CSNOWsurface = (int)column.Value;
                                continue;
                            }
                            else if (column.ParameterName == "192")
                            {
                                newRow.CRAINsurface = (int)column.Value;
                                continue;
                            }
                            else if (column.ParameterName == "Temperature")
                            {
                                newRow.TMPsurface = column.Value;
                                continue;
                            }
                        }
                        else if (column.Level == 2)
                        {
                            if (column.ParameterName == "Temperature")
                            {
                                newRow.TMP2mAboveGround = column.Value;
                                continue;
                            }
                            if (column.ParameterName == "Relative humidity")
                            {
                                newRow.RH2mAboveGround = column.Value;
                                continue;
                            }
                        }
                        else if (column.Level == 10)
                        {
                            if (column.ParameterName == "u-component of wind")
                            {
                                newRow.UGRD10m = column.Value;
                                continue;
                            }
                            else if (column.ParameterName == "v-component of wind")
                            {
                                newRow.VGRD10m = column.Value;
                                continue;
                            }
                        }
                        else if (column.Level == 80)
                        {
                            if (column.ParameterName == "Temperature")
                            {
                                newRow.TMP80mAboveGround = column.Value;
                                continue;
                            }
                            else if (column.ParameterName == "u-component of wind")
                            {
                                newRow.UGRD80m = column.Value;
                                continue;
                            }
                            else if (column.ParameterName == "v-component of wind")
                            {
                                newRow.VGRD80m = column.Value;
                                continue;
                            }
                        }
                    }
                    else if (column.TypeOfLevel == "tropopause")
                    {
                        if (column.ParameterName == "Temperature")
                        {
                            newRow.TMPTrop = column.Value;
                            continue;
                        }
                        else if (column.ParameterName == "u-component of wind")
                        {
                            newRow.UGRDTrop = column.Value;
                            continue;
                        }
                        else if (column.ParameterName == "v-component of wind")
                        {
                            newRow.VGRDTrop = column.Value;
                            continue;
                        }
                    }
                    throw new NotSupportedException($"unknown parameter level: {column.Level} name: {column.ParameterName}");
                }
                namTable.Add(newRow);
            }

            return namTable;
        }

        internal static bool TryFindBootstrapLibrary(out string path)
        {
            path = "";

            // TODO: make cross platform
            string binaryType = "dll";
            string file = "Grib.Api.Native." + binaryType;

            const string PATH_TEMPLATE = "Grib.Api\\lib\\win";
            string platform = (IntPtr.Size == 8) ? "x64" : "x86";
            string gribNativeLibPath = Path.Combine(PATH_TEMPLATE, platform, file);

            return TryBuildDescriptorPath(gribNativeLibPath, out path);
        }

        internal static bool TryFindDefinitions(out string path)
        {
            return TryBuildDescriptorPath("Grib.Api\\definitions", out path);
        }

        internal static bool TryBuildDescriptorPath(string target, out string path)
        {
            path = "";
            target += "";
            string varDef = Environment.GetEnvironmentVariable("GRIB_API_DIR_ROOT") + "";

            string envDir = Path.Combine(varDef, target);
            string thisDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "", target);
            string baseDomainDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "", target);
            string relDomainDir = Path.Combine(AppDomain.CurrentDomain.RelativeSearchPath + "", target);

            return TryBuildGriApiPath(thisDir, out path) ||
                   TryBuildGriApiPath(relDomainDir, out path) ||      // try using the directory that contains this binary
                   TryBuildGriApiPath(baseDomainDir, out path) ||     // try using the directory that contains the exe
                   TryBuildGriApiPath(envDir, out path) ||            // try using environment variable
                   TryBuildGriApiPath(target, out path);              // try using relative path;      
        }
        internal static bool TryBuildGriApiPath(string root, out string path)
        {
            path = "";

            if (File.Exists(root) || Directory.Exists(root))
            {
                path = Path.GetFullPath(root);
            }

            return !String.IsNullOrWhiteSpace(path);
        }
    }
}
