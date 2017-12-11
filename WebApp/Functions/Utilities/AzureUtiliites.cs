using Microsoft.Azure;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using System.IO;

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