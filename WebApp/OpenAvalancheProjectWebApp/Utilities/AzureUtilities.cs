using Microsoft.Azure;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace OpenAvalancheProjectWebApp.Utilities
{
    public static class AzureUtilities
    {
        private static DataLakeStoreFileSystemManagementClient adlsClient;
        public static DataLakeStoreFileSystemManagementClient AdlsClient
        {
            get
            {
                if (adlsClient == null)
                {
                    var adlsAccountName = WebConfigurationManager.AppSettings["ADLSAccountName"];

                    //auth secrets 
                    var domain = WebConfigurationManager.AppSettings["Domain"];
                    var webApp_clientId = WebConfigurationManager.AppSettings["WebAppClientId"];
                    var clientSecret = WebConfigurationManager.AppSettings["ClientSecret"];
                    var clientCredential = new ClientCredential(webApp_clientId, clientSecret);
                    var creds = Task.Run(async () => { return await ApplicationTokenProvider.LoginSilentAsync(domain, clientCredential); }).Result;
                    //var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientCredential).Result;

                    // Create client objects and set the subscription ID
                    var adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);
                    adlsClient = adlsFileSystemClient;
                }
                return adlsClient;
            }
        }

        private static CloudBlobClient blobClient;
        public static CloudBlobClient CloudBlobClient
        {
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