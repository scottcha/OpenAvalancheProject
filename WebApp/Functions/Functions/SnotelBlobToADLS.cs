using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.DataLake.Store;
using System.Text;
using System;
using Microsoft.Azure;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using OpenAvalancheProject.Pipeline.Utilities;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class SnotelBlobToADLS
    {
        [FunctionName("SnotelBlobToADLS"), Disable()]
        [return: Table("snoteltracker")]
        public static FileProcessedTracker Run([BlobTrigger("snotel-csv-westus-v1/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name, TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            log.Info($"Double Checking if {name} already exists.");
            var exists = AzureUtilities.CheckIfFileProcessedRowExistsInTableStorage(Constants.SnotelTrackerTable, Constants.SnotelTrackerPartitionKey, name, log);
            if(exists)
            {
                log.Info($"{name} Already exists in double check, skipping");
                return null;
            }
            //1. Remove header from stream 
            var s = new MemoryStream();
            StreamWriter csvWriter = new StreamWriter(s, Encoding.UTF8);
            bool firstLine = true;
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
                        
                        //Dates in the file are local times; need to change them to UTC
                        var splitLine = line.Split(',');
                        if (firstLine == false && splitLine.Length > 1)
                        {
                            var localTimeOfForecast = DateTime.Parse(splitLine[0]);
                            var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                            var utcTimeOfForecast = TimeZoneInfo.ConvertTimeToUtc(localTimeOfForecast, localTimeZone);
                            splitLine[0] = utcTimeOfForecast.ToString("yyyyMMdd HH:00");
                            line = String.Join(",", splitLine);
                        }
                        firstLine = false;
                        csvWriter.WriteLine(line);
                    }
                }
            }
            csvWriter.Flush();
            s.Position = 0;

            //refactoring the below code to a shared method can cause an .net issue 
            //related to binding redirect to arise; leave this here for now.  See AzureUtilities.cs 
            //for more info
            log.Info($"Attempting to sign in to ad for datalake upload");
            var adlsAccountName = CloudConfigurationManager.GetSetting("ADLSAccountName");

            //auth secrets 
            var domain = CloudConfigurationManager.GetSetting("Domain");
            var webApp_clientId = CloudConfigurationManager.GetSetting("WebAppClientId");
            var clientSecret = CloudConfigurationManager.GetSetting("ClientSecret");
            var clientCredential = new ClientCredential(webApp_clientId, clientSecret);
            var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientCredential).Result;

            // Create client objects and set the subscription ID
            var adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);
            //string subId = CloudConfigurationManager.GetSetting("SubscriptionId");

            try
            {
                adlsFileSystemClient.FileSystem.Create(adlsAccountName, "/snotel-csv-westus-v1/" + name, s, overwrite: true);
                log.Info($"Uploaded csv stream: {name}");
            }
            catch (Exception e)
            {
                log.Info($"Upload failed: {e.Message}");
            }
            var splitFileName = name.Split('.');
            DateTime date = DateTime.ParseExact(splitFileName[0], "yyyyMMdd", null).AddHours(int.Parse(splitFileName[1]));
            return new FileProcessedTracker { ForecastDate = date, PartitionKey = "snotel-csv-westus-v1", RowKey = name, Url = "unknown" };
        }
    }
}