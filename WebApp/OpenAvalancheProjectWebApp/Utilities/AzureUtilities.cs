using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpenAvalancheProjectWebApp.Utilities
{
    public static class AzureUtilities
    {
        private static CloudBlobClient blobClient;
        public static CloudBlobClient CloudBlobClient{
            get
            {
                if (AzureUtilities.blobClient == null)
                {
                    var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("oapstoragedevelop_AzureStorageConnectionString"));
                    blobClient = storageAccount.CreateCloudBlobClient();
                }
                return blobClient;
            }
        }

        private static CloudTableClient tableClient;
        public static CloudTableClient CloudTableClient
        {
            get
            {
                if (AzureUtilities.tableClient == null)
                {

                    // Retrieve storage account from connection string.
                    var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("oapstoragedevelop_AzureStorageConnectionString"));
                    tableClient = storageAccount.CreateCloudTableClient();
                }
                return tableClient;
            }
        }

        public static CloudBlobContainer FeaturesBlobContainer
        {
            get
            {
                CloudBlobContainer container = CloudBlobClient.GetContainerReference("features-csv-westus-v1");
                return container;
            }
        }

        public static CloudBlobContainer ModelBlobContainer
        {
            get
            {
                CloudBlobContainer container = CloudBlobClient.GetContainerReference("models");
                return container;
            }
        }
    }
}