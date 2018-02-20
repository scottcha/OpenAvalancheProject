using System;
using Microsoft.Azure;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using OpenAvalancheProject.Pipeline.Utilities;
using Microsoft.Azure.DataLake.Store;
using System.IO;
using System.Collections.Generic;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class MergeStateSnotelFiles
    {
        static string partitionName = "snotel-merged-csv-westus-v1";
        static string csvDirectory = "/snotel-csv-westus-v1/";

        [FunctionName("MergeStateSnotelFiles"), Disable()]
        public static void Run([TimerTrigger("0 30 * * * *", RunOnStartup = true)]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            int numberOfStates = 12;
            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("snotelmergetracker");
            table.CreateIfNotExists();
#if DEBUG
            int numberOfHoursToCheck = 3*7*24; //was  1;
#else
            int numberOfHoursToCheck = 7*24; //one week
#endif
            //look back x days and fill in any missing values; 
            TableQuery<FileProcessedTracker> dateQuery = new TableQuery<FileProcessedTracker>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate("ForecastDate", QueryComparisons.GreaterThan, DateTime.UtcNow.AddDays(-1 * numberOfHoursToCheck))
                )
            );

            var results = table.ExecuteQuery(dateQuery);

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
            var fullAdlsAccountName = CloudConfigurationManager.GetSetting("ADLSFullAccountName");

            // Create client objects and set the subscription ID
            var adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);
            var adlsClient = AdlsClient.CreateClient(fullAdlsAccountName, creds);
            var checkDate = DateTime.UtcNow.AddHours(-1 * numberOfHoursToCheck);
            while(checkDate < DateTime.UtcNow)
            {
                //has it already been marked as complete in the table
                string nameToCheck = SnotelUtilities.CreateSnotelFileDate(checkDate) + ".snotel.csv";
                if (results.Where(r => r.RowKey == nameToCheck).Count() == 0)
                {
                    log.Info($"{nameToCheck} doesn't exist in completed table, need to see if all files exist to concat");
                    var lexStartAndEnd = SnotelUtilities.CreateSnotelFileDate(checkDate);
                    var hourlyFilesOnAdls = adlsClient.EnumerateDirectory(csvDirectory).Where(f => f.Name.StartsWith(lexStartAndEnd)).Select(f => f.Name).ToList();
                    if(hourlyFilesOnAdls.Count == numberOfStates)
                    {
                        if (ConcatFiles(adlsClient, nameToCheck, hourlyFilesOnAdls))
                        {
                            //mark file as finished in table
                            FileProcessedTracker tracker = new FileProcessedTracker { ForecastDate = checkDate, PartitionKey = partitionName, RowKey = nameToCheck, Url = "unknown" };
                            table.Execute(TableOperation.Insert(tracker));
                        }
                        else
                        {
                            log.Error($"Missing data for {checkDate} need to manually backfill, can't concat");
                        }
                    }
                    else
                    {
                        log.Info($"all state files don't exist for {checkDate}, waiting until next run");
                    }
                }
                else
                {
                    log.Info($"{nameToCheck} marked as already concated");
                }
                checkDate = checkDate.AddHours(1);
            }
        }

        private static bool ConcatFiles(AdlsClient adlsClient, string nameToCheck, List<string> hourlyFilesOnAdls)
        {
            //all file exist and can be concated
            List<string> lines = new List<string>();
            bool firstFile = true;
            foreach (var f in hourlyFilesOnAdls)
            {
                var state = f.Split('.')[2];
                int linesToSkip = 0;
                if (firstFile)
                {
                    linesToSkip = 0;
                }
                else
                {
                    linesToSkip = 1;
                }
                using (var readStream = new StreamReader(adlsClient.GetReadStream(csvDirectory + f)))
                {
                    string line;
                    while ((line = readStream.ReadLine()) != null)
                    {
                        if (linesToSkip == 1)
                        {
                            linesToSkip = 0;
                            continue;
                        }
                        if (firstFile)
                        {
                            lines.Add(line);
                            firstFile = false;
                        }
                        else
                        {
                            lines.Add(line + "," + state);
                        }
                    }
                }
            }
            if(lines.Count == 0)
            {
                return false;
            }
            lines[0] = lines[0] + ",State";
            using (var streamWriter = new StreamWriter(adlsClient.CreateFile(@"/" + partitionName + @"/" + nameToCheck, IfExists.Overwrite)))
            {
                foreach (var l in lines)
                {
                    streamWriter.WriteLine(l);
                }
            }
            return true;
            
        }
    }
}
