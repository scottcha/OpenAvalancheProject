using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class DownloadSnotelToBlob
    {
        //execute every hour at 10 after; hourly snotel data usually available by then
        [FunctionName("DownloadSnotelToBlob")]
        public static void Run([TimerTrigger("0 10 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            //%HOUR% & %STATE% to be populated
            string snotelTemplate = @"https://wcc.sc.egov.usda.gov/reportGenerator/view_csv/customMultipleStationReport/hourly/start_of_period/state=%22%STATE%%22%20AND%20network=%22SNTLT%22,%22SNTL%22%20AND%20element=%22SNWD%22%20AND%20outServiceDate=%222100-01-01%22%7Cname/-23,0:H%7C%HOUR%/name,elevation,latitude,longitude,WTEQ::value,PREC::value,SNWD::value,TOBS::value?fitToScreen=false";
            string[] stateList = { "WA", "OR", "CA", "ID", "UT", "NV", "MT", "WY", "CO", "AZ", "NM", "AK" };

            string partitionName = "snotel-westus-v1";

            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("snoteltracker");
            table.CreateIfNotExists();

            //look back eight days and fill in any missing values; I beleive they store files on this server for 7 days
            TableQuery<FileProcessedTracker> dateQuery = new TableQuery<FileProcessedTracker>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate("SnotelDate", QueryComparisons.GreaterThan, DateTime.UtcNow.AddDays(-8))
                )
            );

        }
    }
}
