using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Azure;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using OpenAvalancheProject.Pipeline.Utilities;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class DownloadAndUnpackSnodas
    {
        [FunctionName("DownloadAndUnpackSnodas"), Disable()]
        [StorageAccount("AzureWebJobsStorage")]
        [return: Table("snodasdownloadtracker")]
        public static FileProcessedTracker Run([QueueTrigger("downloadandunpacksnodas", Connection = "AzureWebJobsStorage")]FileReadyToDownloadQueueMessage myQueueItem,
                               TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed snodas date: {myQueueItem.FileDate}");
            string partitionName = myQueueItem.Filetype;
            
            var urlToDownload = myQueueItem.Url;
            log.Info($"Downloading Url {urlToDownload}");

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(urlToDownload);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            // This example assumes the FTP site uses anonymous logon.  
            request.Credentials = new NetworkCredential("anonymous", "");
            List<string> listOfUnpackedFiles = null;
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            log.Info($"File {urlToDownload} downloaded");
            Stream responseStream = response.GetResponseStream();
            listOfUnpackedFiles = SnodasUtilities.UnpackSnodasStream(responseStream);
            log.Info($"File {urlToDownload} unpacked");
            
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
            log.Info($"Attempting to upload unpacked files to adls");
#if DEBUG
//            listOfUnpackedFiles = listOfUnpackedFiles.Where(f => f.Contains(".Hdr")).Take(1).ToList();
#endif
            foreach (var file in listOfUnpackedFiles)
            {
                try
                {
                    adlsFileSystemClient.FileSystem.UploadFile(adlsAccountName, file, "/snodas-dat-us-v1/" + file.Split('\\').Last(), uploadAsBinary: true, overwrite: true);
                    log.Info($"Uploaded file: {file}");
                }
                catch (Exception e)
                {
                    log.Error($"Upload failed: {e.Message}");
                }
            }

            //1: Get values for lat/lon
            var locations = AzureUtilities.DownloadLocations(log);
#if DEBUG
            var executingAssemblyFile = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath;
            var executingDirectory = Path.GetDirectoryName(executingAssemblyFile);

            if (string.IsNullOrEmpty(executingDirectory))
                throw new InvalidOperationException("cannot get executing directory");
            executingDirectory = Directory.GetParent(executingDirectory).FullName;

            var gdalPath = Path.Combine(executingDirectory, "gdal");
            log.Info($"Have gdal path {gdalPath}");
#endif
            log.Info($"Configuring gdal");
            GdalConfiguration.ConfigureGdal();
            var results = SnodasUtilities.GetValuesForCoordinates(locations, listOfUnpackedFiles.Where(f => f.Contains(".Hdr")).ToList());
            log.Info($"Have {results.Count} results for coordinates.");
            DateTime fileDate;
            string fileName;
            using (MemoryStream s = new MemoryStream())
            using (StreamWriter csvWriter = new StreamWriter(s, Encoding.UTF8))
            {
                csvWriter.WriteLine(SnodasRow.GetHeader);
                foreach(var row in results)
                {
                    csvWriter.WriteLine(row.ToString());
                }
                csvWriter.Flush();
                s.Position = 0;

                fileDate = results[0].Date;
                fileName = fileDate.ToString("yyyyMMdd") + "Snodas.csv";
                try
                {
                    adlsFileSystemClient.FileSystem.Create(adlsAccountName, "/snodas-csv-westus-v1/" + fileName, s, overwrite: true);
                    log.Info($"Uploaded csv stream: {fileName}");
                }
                catch (Exception e)
                {
                    log.Info($"Upload failed: {e.Message}");
                }
            }

            log.Info($"Removing unpacked files");
            foreach(var f in listOfUnpackedFiles)
            {
                //delete local temp file
                File.Delete(f);
            }
            return new FileProcessedTracker { ForecastDate = fileDate, PartitionKey = "snodas-westus-v1", RowKey = fileName, Url = "unknown" };
        }
    }
}