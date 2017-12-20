/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Azure;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.WindowsAzure.Storage.Blob;
using OpenAvalancheProject.Pipeline.Utilities;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class DownloadAndUnpackSnodas
    {
        [FunctionName("DownloadAndUnpackSnodas")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run([QueueTrigger("downloadandunpacksnodas", Connection = "AzureWebJobsStorage")]FileReadyToDownloadQueueMessage myQueueItem,
                               TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");
            string partitionName = myQueueItem.Filetype;

            var urlToDownload = myQueueItem.Url;
            log.Info($"Downloading Url {urlToDownload}");

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(urlToDownload);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            // This example assumes the FTP site uses anonymous logon.  
            request.Credentials = new NetworkCredential("anonymous", "");
            List<string> listOfUnpackedFiles = null;
            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                listOfUnpackedFiles = SnodasUtilities.UnpackSnodasStream(responseStream);
            }
            catch (WebException ex)
            {
                log.Error($"Exception on snodas ftp download for url {urlToDownload} with exception message {ex.Message}");
            }
            
            //fix the bard codes in the hdr files
            foreach(var f in listOfUnpackedFiles.Where(s => s.ToLower().Contains(".hdr")))
            {
                SnodasUtilities.RemoveBardCodesFromHdr(f);
            }

            log.Info($"Attempting to sign in to ad for datalake upload");
            var adlsAccountName = CloudConfigurationManager.GetSetting("ADLSAccountName");

            //auth secrets 
            var domain = CloudConfigurationManager.GetSetting("Domain");
            var webApp_clientId = CloudConfigurationManager.GetSetting("WebAppClientId");
            var clientSecret = CloudConfigurationManager.GetSetting("ClientSecret");
            var clientCredential = new ClientCredential(webApp_clientId, clientSecret);
            var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientCredential).Result;

            // Create client objects and set the subscription ID
            var adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);
            foreach (var file in listOfUnpackedFiles)
            {
                try
                {
                    adlsFileSystemClient.FileSystem.UploadFile(adlsAccountName, file, "/snodas-dat-us-v1/" + file, uploadAsBinary: true, overwrite: true);
                    log.Info($"Uploaded file: {file}");
                }
                catch (Exception e)
                {
                    log.Error($"Upload failed: {e.Message}");
                }
            }
        }
    }
}
*/