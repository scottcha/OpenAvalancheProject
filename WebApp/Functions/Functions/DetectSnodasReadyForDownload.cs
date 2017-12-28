using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class DetectSnodasReadyForDownload
    {
        /// <summary>
        /// Detect if a new snodas file is available; run every 12 hours starting at 6:10am pst/2:10pm utc; files are dropped at 6am 
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        [FunctionName("DetectSnodasReadyForDownload")]
        [return: Queue("downloadandunpacksnodas")]
        public static void Run([TimerTrigger("0 10 3/3 1/1 * *", RunOnStartup = true)]TimerInfo myTimer,
                               [Queue("downloadandunpacksnodas", Connection = "AzureWebJobsStorage")] ICollector<FileReadyToDownloadQueueMessage> outputQueueItem,
                               TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            string partitionName = "snodas-westus-v1";
            log.Info($"DetectSnodasReadyForDownload Timer trigger function executed at UTC: {DateTime.UtcNow}");

            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("snodasdownloadtracker");
            table.CreateIfNotExists();

            //look back eight days and fill in any missing values; I beleive they store files on this server for 7 days
            TableQuery<FileProcessedTracker> dateQuery = new TableQuery<FileProcessedTracker>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate("ForecastDate", QueryComparisons.GreaterThan, DateTime.UtcNow.AddDays(-8))
                )
            );

            var results = table.ExecuteQuery(dateQuery);
#if DEBUG
            int numberOfDaysToCheck = 1;
#else
            int numberOfDaysToCheck = 5;
#endif
            //1. Are there any missing dates for the last n days we should backfill
            var currentDate = DateTime.UtcNow.AddDays(-1 * numberOfDaysToCheck);
            var checkDate = currentDate;
            var listOfDatesToDownload = new List<DateTime>();
            while (checkDate < DateTime.UtcNow)
            {
                string fileName =  checkDate.ToString("yyyyMMdd") + "Snodas.csv";
                if (results.Where(r => r.RowKey == fileName).Count() == 0)
                {
                    //If file doesn't exist enter a new item
                    log.Info($"snodas backfill: adding item {fileName} to download queue");
                    listOfDatesToDownload.Add(checkDate.Date);
                }
                else
                {
                    log.Info($"Skipping item {fileName} as it already exists");
                }
                checkDate = checkDate.AddDays(1);
            }

            foreach (var date in listOfDatesToDownload)
            {
                // Get the object used to communicate with the server.  
                var urlBase = @"ftp://sidads.colorado.edu/pub/DATASETS/NOAA/G02158/masked/";
                urlBase += date.ToString("yyyy/");
                urlBase += date.ToString("MM_MMM/");
                urlBase += "SNODAS_" + date.ToString("yyyyMMdd") + ".tar";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(urlBase);
                request.Method = WebRequestMethods.Ftp.GetDateTimestamp;

                // This example assumes the FTP site uses anonymous logon.  
                request.Credentials = new NetworkCredential("anonymous", "");
                try
                {
                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                    //file exists; add to download queue 
                    log.Info($"Adding snodas file with {date} to download queue.");
                    //enter a new queue item 
                    outputQueueItem.Add(new FileReadyToDownloadQueueMessage { FileName = "SNODAS_"+ date.ToString("yyyyMMdd") + "tar",
                                                                              FileDate = date.ToString("yyyyMMdd"), Url = urlBase,
                                                                              Filetype = partitionName });
                }
                catch (WebException ex)
                {
                    FtpWebResponse response = (FtpWebResponse)ex.Response;
                    if (response.StatusCode ==
                        FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        log.Info($"SNODAS File for date {date} not available, skipping.");
                        //Does not exist
                        continue;
                    }
                    else
                    {
                        log.Error($"Error attempting to see if snodas file with date {date} exists on ftp server.");
                    }
                }
            }
            //Stream responseStream = response.GetResponseStream();
            //StreamReader reader = new StreamReader(responseStream);
            //string line = null;
            //while ((line = reader.ReadLine()) != null)
            //{
            //    if(line.Contains("SNODAS_") && line.Contains(".tar"))
            //    {
            //        var datePart = line.Split('_')[1].Split('.')[0];
            //        //TODO: take this; determine if its in the previously downloaded list; otherwise download it                    

            //    }
            //    log.Info($"Have directory contents: {line}");
            //}
        }
    }
}