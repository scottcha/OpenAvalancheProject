using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;

namespace OpenAvalancheProject.Pipeline.Functions
{
    //TODO: test this
    public static class DetectSnotelReadyForDownload
    {
        [FunctionName("DetectSnotelReadyForDownload")]
        [return: Queue("filereadytodownloadqueue")]
        public static void Run([TimerTrigger("0 10 * * * *")]TimerInfo myTimer, [Queue("snotelreadytodownloadqueue", Connection = "AzureWebJobsStorage")] ICollector<FileReadyToDownloadQueueMessage> outputQueueItem, 
                                TraceWriter log)
        {
#if DEBUG
            int numberOfDaysToCheck = 1;
#else
            int numberOfDaysToCheck = 2;
#endif

            log.Info($"C# DetectSnotelReadyForDownload Timer trigger function executed at: {DateTime.Now}");
            //%HOUR% & %STATE% to be populated
            string snotelTemplate = @"https://wcc.sc.egov.usda.gov/reportGenerator/view_csv/customMultipleStationReport/hourly/start_of_period/state=%22%STATE%%22%20AND%20network=%22SNTLT%22,%22SNTL%22%20AND%20element=%22SNWD%22%20AND%20outServiceDate=%222100-01-01%22%7Cname/-23,0:H%7C%HOUR%/name,elevation,latitude,longitude,WTEQ::value,PREC::value,SNWD::value,TOBS::value?fitToScreen=false";
            string[] stateList = { "WA", "OR", "CA", "ID", "UT", "NV", "MT", "WY", "CO", "AZ", "NM", "AK" };

            string partitionName = "snotel-westus-v1";

            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("snoteltracker");
            table.CreateIfNotExists();

            //look back eight days and fill in any missing values; 
            TableQuery<FileProcessedTracker> dateQuery = new TableQuery<FileProcessedTracker>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate("ForecastDate", QueryComparisons.GreaterThan, DateTime.UtcNow.AddDays(-1 * numberOfDaysToCheck))
                )
            );

            var results = table.ExecuteQuery(dateQuery);

            //1. Are there any missing dates for the last n days we should backfill
            var checkDate = DateTime.UtcNow.AddDays(-1 * numberOfDaysToCheck);
#if DEBUG
            stateList = new string[] { stateList[0] };
#endif
            while(checkDate < DateTime.UtcNow)
            {
                foreach(var state in stateList)
                {
                    string fileName = CreateSnotelFilename(checkDate);
                    if (results.Where(r => r.RowKey == fileName).Count() == 0)
                    {
                        var snotelUrl = snotelTemplate.Replace("%STATE%", state);
                        snotelUrl = snotelUrl.Replace("%HOUR%", checkDate.ToString("h"));
                        log.Info($"Adding file {fileName} with date {checkDate} to download queue.");
                        //enter a new queue item for every file missing
                        outputQueueItem.Add(new FileReadyToDownloadQueueMessage { FileName = fileName, FileDate = checkDate.ToString("yyyyMMdd HH:00:00"), Url = snotelUrl, Filetype = partitionName });
                    }
                }
            }

            //2. Add the current hour to the queue
            foreach(var state in stateList)
            {
                var timeNow = DateTime.UtcNow;
                var snotelUrl = snotelTemplate.Replace("%STATE%", state);
                snotelUrl = snotelUrl.Replace("%HOUR%", timeNow.ToString("h"));
                var fileName = CreateSnotelFilename(timeNow);
                log.Info($"Adding file {fileName} with date {checkDate} to download queue.");
                //enter a new queue item for every file missing
                outputQueueItem.Add(new FileReadyToDownloadQueueMessage { FileName = fileName, FileDate = checkDate.ToString("yyyyMMdd HH:00:00"), Url = snotelUrl, Filetype = partitionName });
            }
        }

        private static string CreateSnotelFilename(DateTime checkDate)
        {
            return checkDate.ToString("yyyyMMdd") + "." + checkDate.ToString("HH") + ".snotel.csv";
        }
    }
}
