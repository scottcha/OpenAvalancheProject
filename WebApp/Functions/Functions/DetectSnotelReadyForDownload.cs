using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using OpenAvalancheProject.Pipeline.Utilities;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class DetectSnotelReadyForDownload
    {
        [FunctionName("DetectSnotelReadyForDownload")]
        [return: Queue("filereadytodownloadqueue")]
        public static void Run([TimerTrigger("0 20 * * * *", RunOnStartup = true)]TimerInfo myTimer, 
                               [Queue("filereadytodownloadqueue", Connection = "AzureWebJobsStorage")] ICollector<FileReadyToDownloadQueueMessage> outputQueueItem, 
                               TraceWriter log)
        {
#if DEBUG
            int numberOfDaysToCheck = 1;
#else
            int numberOfDaysToCheck = 5;
#endif

            log.Info($"C# DetectSnotelReadyForDownload Timer trigger function executed at: {DateTime.Now}");
            //%HOUR% & %STATE% to be populated
            string[] stateList = { "WA", "OR", "CA", "ID", "UT", "NV", "MT", "WY", "CO", "AZ", "NM", "AK" };

            string partitionName = "snotel-csv-westus-v1";

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
            var checkDate = DateTime.UtcNow.AddDays(-1 * numberOfDaysToCheck);;
#if DEBUG
            stateList = new string[] { stateList[0] };
#endif
            while(checkDate < DateTime.UtcNow)
            {
                foreach(var state in stateList)
                {
                    string fileName = CreateSnotelFileDate(checkDate) + "." + state + ".snotel.csv";
                    if (results.Where(r => r.RowKey == fileName).Count() == 0)
                    {
                        //If file doesn't exist enter a new item
                        log.Info($"backfill: adding item {fileName} to download queue");
                        CreateQueueItem(outputQueueItem, log, partitionName, checkDate, state);
                    }
                    else
                    {
                        log.Info($"Skipping item {fileName} as it already exists");
                    }
                }
                checkDate = checkDate.AddHours(1);
            }
        }

        private static void CreateQueueItem(ICollector<FileReadyToDownloadQueueMessage> outputQueueItem, 
                                            TraceWriter log, 
                                            string partitionName, 
                                            DateTime readingDateUtc, string state)
        {
            string snotelTemplate = @"https://wcc.sc.egov.usda.gov/reportGenerator/view_csv/customMultipleStationReport/hourly/start_of_period/state=%22%STATE%%22%20AND%20network=%22SNTLT%22,%22SNTL%22%20AND%20element=%22SNWD%22%20AND%20outServiceDate=%222100-01-01%22%7Cname/%yyyy-MM-dd%,%yyyy-MM-dd%:H%7C%HOUR%/name,elevation,latitude,longitude,WTEQ::value,PREC::value,SNWD::value,TOBS::value";
            string snotelUrl = CreateSnotelUrl(readingDateUtc, state, snotelTemplate);
            //keep the file date utc; we'll correct the times in the file to UTC in the ADSL upload
            var fileDate = CreateSnotelFileDate(readingDateUtc);
            log.Info($"Adding file {fileDate} with state {state} to download queue.");
            //enter a new queue item 
            outputQueueItem.Add(new FileReadyToDownloadQueueMessage { FileName = state + ".snotel.csv", FileDate = fileDate, Url = snotelUrl, Filetype = partitionName });
        }

        public static string CreateSnotelUrl(DateTime readingDateUtc, string state, string snotelTemplate)
        {
            var snotelUrl = snotelTemplate.Replace("%STATE%", state);
            //Date is utc, need to make it local to the request location
            TimeZoneInfo timeInfo = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            var readingDateLocal = TimeZoneInfo.ConvertTimeFromUtc(readingDateUtc, timeInfo);
            var tmpDate = readingDateLocal.ToString("yyyy-MM-dd");
            snotelUrl = snotelUrl.Replace("%yyyy-MM-dd%", tmpDate);
            //odd case where you need to include a whitespace to get this to work per C# docs
            var tmpHour = readingDateLocal.ToString("H ").Trim(' ');
            snotelUrl = snotelUrl.Replace("%HOUR%", tmpHour);
            return snotelUrl;
        }

        private static string CreateSnotelFileDate(DateTime checkDate)
        {
            return checkDate.ToString("yyyyMMdd") + "." + checkDate.ToString("HH");
        }
    }
}