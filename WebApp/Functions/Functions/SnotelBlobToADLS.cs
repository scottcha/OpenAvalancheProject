using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using OpenAvalancheProject.Pipeline.Utilities;
using Microsoft.Azure.Management.DataLake.Store;
using System.Text;
using System;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class SnotelBlobToADLS
    {
        [FunctionName("SnotelBlobToADLS")]
        [return: Table("snoteltracker")]
        public static FileProcessedTracker Run([BlobTrigger("snotel-westus-v1/{name}", Connection = "AzureWebStorage")]Stream myBlob, string name, TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            //1. Remove header from stream 
            var s = new MemoryStream();
            StreamWriter csvWriter = new StreamWriter(s, Encoding.UTF8);
            using (StreamReader sr = new StreamReader(myBlob))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("#"))
                    {
                        //throw out this header
                        continue;
                    }
                    else
                    {
                        csvWriter.WriteLine(line);
                    }
                }
            }
            csvWriter.Flush();
            s.Position = 0;

            AzureUtilities.AuthenticateADLSFileSystemClient(out DataLakeStoreFileSystemManagementClient adlsFileSystemClient, 
                                                            out string adlsAccountName, 
                                                            log);

            try
            {
                adlsFileSystemClient.FileSystem.Create(adlsAccountName, "/csv-westus-v1/" + name, s, overwrite: true);
                log.Info($"Uploaded csv stream: {name}");
            }
            catch (Exception e)
            {
                log.Info($"Upload failed: {e.Message}");
            }

            var splitFileName = name.Split('.');
            DateTime date = DateTime.Parse(splitFileName[0]).AddHours(int.Parse(splitFileName[1]));
            return new FileProcessedTracker { ForecastDate = date, PartitionKey = "csv-westus-v1", RowKey = name, Url = "unknown" };
        }
    }
}
