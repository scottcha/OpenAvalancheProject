using Microsoft.Azure;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenAvalancheProject.Pipeline.Utilities
{
    public static class AzureUtilities
    {
        public static string DownloadBlobToTemp(Stream myBlob, string name, TraceWriter log)
        {
            var localFileName = Path.GetTempPath() + name;
            using (var fileStream = File.Create(localFileName))
            {
                myBlob.Seek(0, SeekOrigin.Begin);
                myBlob.CopyTo(fileStream);
                log.Info($"Copied file to {localFileName}");
            }

            return localFileName;
        }

        public static void UploadLocationsFile(Stream locationsStream, TraceWriter log)
        {
            log.Info("Uploading locations file");
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(Constants.LatLonCacheContainerName);
            container.CreateIfNotExists();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(Constants.LatLonCacheFileName);
            // Create or overwrite the "myblob" blob with contents from a local file.
            blockBlob.UploadFromStream(locationsStream);
            log.Info("Completed Uploading locations file");
        }

        public static List<(double, double)> DownloadLocations(TraceWriter log)
        {
            log.Info("Downloading locations file");
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(Constants.LatLonCacheContainerName);
            container.CreateIfNotExists();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(Constants.LatLonCacheFileName);
            var latLonList = new List<(double, double)>();
            using (MemoryStream s = new MemoryStream())
            {
                blockBlob.DownloadToStream(s);
                s.Position = 0;
                using (StreamReader sr = new StreamReader(s))
                {
                    string line = null;
                    bool firstLine = true;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if(firstLine)
                        {
                            //there is a header, so skip that
                            firstLine = false;
                            continue;
                        }
                        var latLon = line.Split(',');
                        latLonList.Add((double.Parse(latLon[0]), double.Parse(latLon[1])));
                    }
                }
            }
            log.Info("Completed downloading and parsing locations file");
            return latLonList;
        }

        public static bool CheckIfFileProcessedRowExistsInTableStorage(string TableName, string PartitionKey, string RowKey, TraceWriter log)
        {
            var value = CloudConfigurationManager.GetSetting("AzureWebJobsStorage");
            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(TableName);
            if (!table.Exists())
            {
                log.Info("Table doesn't exist");
                return false;
            }

            TableOperation retrieveOperation = TableOperation.Retrieve<FileProcessedTracker>(PartitionKey, RowKey);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result == null)
            {
                log.Info("Result doesn't exist");
                return false;
            }
            else
            {
                log.Info("Result exists");
                return true;
            }
/*
            TableQuery<FileProcessedTracker> query = new TableQuery<FileProcessedTracker>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, RowKey)
                )
            );

            var results = table.ExecuteQuery(query);
            if(results.Count() == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
            */
        }
        //https://github.com/dotnet/sdk/issues/1405 is blocking this from being refactored to a shared 
        //function
        /*
        public static void AuthenticateADLSFileSystemClient(out DataLakeStoreFileSystemManagementClient adlsFileSystemClient, out string adlsAccountName, TraceWriter log)
        {
            log.Info($"Attempting to sign in to ad for datalake upload");
            adlsAccountName = CloudConfigurationManager.GetSetting("ADLSAccountName");

            //auth secrets 
            var domain = CloudConfigurationManager.GetSetting("Domain");
            var webApp_clientId = CloudConfigurationManager.GetSetting("WebAppClientId");
            var clientSecret = CloudConfigurationManager.GetSetting("ClientSecret");
            var clientCredential = new ClientCredential(webApp_clientId, clientSecret);
            var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientCredential).Result;

            // Create client objects and set the subscription ID
            adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);
            //string subId = CloudConfigurationManager.GetSetting("SubscriptionId");
        }
        */
    }
}