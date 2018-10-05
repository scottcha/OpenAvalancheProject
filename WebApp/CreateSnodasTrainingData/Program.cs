using OpenAvalancheProject.Pipeline;
using OpenAvalancheProject.Pipeline.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CreateSnodasTrainingData
{
    public class Program
    {
        //static void Main(string[] args)
        //{
        //    var listOfHdrFiles = Directory.GetFiles(@"E:\Data\SnowData\SNODAS\Compressed\");
        //    foreach (var f in listOfHdrFiles.Where(s => s.ToLower().Contains(".hdr")))
        //    {
        //        SnodasUtilities.RemoveBardCodesFromHdr(f);
        //    }
        //}
        static void Main(string[] args)
        {
            //get list of hdr files
            var listOfUnpackedFiles = Directory.GetFiles(@"E:\Data\SnowData\SNODAS\All\");
            //var listOfUnpackedFiles = SnodasUtilities.UnpackSnodasStream(responseStream);

            //fix the bard codes in the hdr files
            //foreach (var f in listOfUnpackedFiles.Where(s => s.ToLower().Contains(".hdr")))
            //{
            //    SnodasUtilities.RemoveBardCodesFromHdr(f);
            //}

            //1: Get values for lat/lon
            //var locations = AzureUtilities.DownloadLocations(log);
            var latLonList = new List<(double, double)>();
            using (StreamReader s = new StreamReader(@"../../LatLonCache.csv"))
            {
                string line = null;
                bool firstLine = true;
                while ((line = s.ReadLine()) != null)
                {
                    if (firstLine)
                    {
                        //there is a header, so skip that
                        firstLine = false;
                        continue;
                    }
                    var latLon = line.Split(',');
                    latLonList.Add((double.Parse(latLon[0]), double.Parse(latLon[1])));
                }
            }
            GdalConfiguration.ConfigureGdal();
            var startDate = new DateTime(2017, 3, 23);
            while (startDate < new DateTime(2018, 1, 1))
            {
                var filteredList = listOfUnpackedFiles.Where(f => f.Contains(startDate.ToString("yyyyMMdd")) && f.Contains(".Hdr")).ToList();

                if (filteredList.Count == 0)
                {
                    startDate = startDate.AddDays(1);
                    continue;
                }
                List<SnodasRow> results = null;
                try
                {
                    results = SnodasUtilities.GetValuesForCoordinates(latLonList, filteredList);
                }
                catch (ApplicationException e)
                {
                    Console.WriteLine("Skipping invalid file with exception: " + e.ToString());
                    startDate = startDate.AddDays(1);
                    continue;
                }
                DateTime fileDate;
                string fileName;
                using (MemoryStream s = new MemoryStream())
                using (StreamWriter csvWriter = new StreamWriter(s, Encoding.UTF8))
                {
                    csvWriter.WriteLine(SnodasRow.GetHeader);
                    foreach (var row in results)
                    {
                        csvWriter.WriteLine(row.ToString());
                    }
                    csvWriter.Flush();
                    s.Position = 0;

                    fileDate = results[0].Date;
                    fileName = fileDate.ToString("yyyyMMdd") + "Snodas.csv";
                    s.CopyTo(new FileStream(@"E:\Data\SnowData\SNODASParsed\2017\" + fileName, FileMode.Create));
                }
                startDate = startDate.AddDays(1);
            }
        }
    }
}
