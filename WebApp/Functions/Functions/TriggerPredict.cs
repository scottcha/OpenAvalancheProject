using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using OpenAvalancheProject.Pipeline.Utilities;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class TriggerPredict
    {
        //create a shared httpclient so as not to exhaust resources
        private static readonly HttpClient client;
        static TriggerPredict()
        {
            client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
        }

        [FunctionName("TriggerPredict")]
        public static void Run([TimerTrigger("0 0 1 1/1 * *", RunOnStartup = true)]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# TriggerPredict trigger function executed at: {DateTime.Now}");

#if DEBUG
            int numberOfDaysToCheck = 7;
#else
            int numberOfDaysToCheck = 7;
#endif

            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(Constants.PredictionTable);
            table.CreateIfNotExists();

            //look back eight days and fill in any missing values; I beleive they store files on this server for 7 days
            TableQuery<FileProcessedTracker> dateQuery = new TableQuery<FileProcessedTracker>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Constants.PredictionTrackerPartitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate("ForecastDate", QueryComparisons.GreaterThanOrEqual, DateTime.UtcNow.AddDays(-1 * numberOfDaysToCheck).Date)
                )
            );


            var results = table.ExecuteQuery(dateQuery);

            //find ones we need to fill
            var checkDate = DateTime.UtcNow.AddDays(-1*numberOfDaysToCheck).Date;
            var listOfDatesToPredict = new List<DateTime>();
            while (checkDate < DateTime.UtcNow)
            {
                string fileName = CreatePredictFileName(checkDate);
                if (results.Where(r => r.RowKey == fileName).Count() == 0)
                {
                    //If file doesn't exist enter a new item
                    log.Info($"prediction backfill: adding item {fileName} to prediction queue");
                    listOfDatesToPredict.Add(checkDate.Date);
                }
                else
                {
                    log.Info($"Skipping item {fileName} as it already exists");
                }
                checkDate = checkDate.AddDays(1);
            }
            //execute the predictions
            foreach (var d in listOfDatesToPredict)
            {
                var uri = Constants.UrlForPredictApi + d.ToString("yyyyMMdd");
                client.GetAsync(uri).Wait();
                log.Info($"predicted {d.ToString("yyyyMMdd")} and setting done marker.");
                var op = TableOperation.InsertOrMerge(new FileProcessedTracker { ForecastDate = d, PartitionKey = Constants.PredictionTrackerPartitionKey, RowKey = CreatePredictFileName(d), Url = "unknown" });
                table.Execute(op);
            }
        }

        private static string CreatePredictFileName(DateTime checkDate)
        {
            return checkDate.ToString("yyyyMMdd") + "prediction";
        }
    }
}
