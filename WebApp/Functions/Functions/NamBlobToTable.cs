using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Grib.Api;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using System;
using Microsoft.Azure;
using System.Collections.Generic;
using System.Text;

using System.Reflection;
namespace OpenAvalancheProject.Pipeline
{
    public static class NAMBlobToTable
    {
        [FunctionName("NAMBlobToTable")]
        [return: Table("filedownloadtracker")]
        public static FileProcessedTracker Run([BlobTrigger("nam-grib-westus-v1/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name, TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            log.Info($"Have env: {Environment.GetEnvironmentVariable("GRIB_API_DIR_ROOT")}");
            log.Info($"In dir: {Assembly.GetExecutingAssembly().Location}");
            string attemptPath = "";
            Utilities.TryFindBootstrapLibrary(out attemptPath);
            log.Info($"Attemping to find lib: {attemptPath}");
#if DEBUG == false
            GribEnvironment.DefinitionsPath = @"D:\home\site\wwwroot\bin\Grib.Api\definitions";
#endif
            GribEnvironment.Init();
            
            //1. Download stream to temp
            var localFileName = Path.GetTempPath() + name;
            using (var fileStream = File.Create(localFileName))
            {
                myBlob.Seek(0, SeekOrigin.Begin);
                myBlob.CopyTo(fileStream);
                log.Info($"Copied file to {localFileName}");
            }

            var rowList = new List<NamTableRow>();
            
            //2. Get values from file
            using (GribFile file = new GribFile(localFileName))
            {
                log.Info($"Parsing file {name}");
                rowList = Utilities.ParseNamGribFile(file);
            }

            //3. Format in correct table format
            log.Info($"Attempting to sign in to ad for datalake upload");
            DataLakeStoreAccountManagementClient _adlsClient;
            DataLakeStoreFileSystemManagementClient _adlsFileSystemClient;

            string _adlsAccountName;
            string _subId;

            _adlsAccountName = CloudConfigurationManager.GetSetting("ADLSAccountName");
            _subId = CloudConfigurationManager.GetSetting("SubscriptionId");

            //auth stuff
            var domain = CloudConfigurationManager.GetSetting("Domain");
            var webApp_clientId = CloudConfigurationManager.GetSetting("WebAppClientId");
            var clientSecret = CloudConfigurationManager.GetSetting("ClientSecret");
            var clientCredential = new ClientCredential(webApp_clientId, clientSecret);
            var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientCredential).Result;

            // Create client objects and set the subscription ID
            _adlsClient = new DataLakeStoreAccountManagementClient(creds) { SubscriptionId = _subId };
            _adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);
            try
            {
                _adlsFileSystemClient.FileSystem.UploadFile(_adlsAccountName, localFileName, "/nam-grib-westus-v1/" + name, uploadAsBinary:true, overwrite:true);
                log.Info($"Uploaded file: {localFileName}");
            }
            catch(Exception e)
            {
                log.Error($"Upload failed: {e.Message}");
            }
          
            MemoryStream s = new MemoryStream();
            StreamWriter csvWriter = new StreamWriter(s, Encoding.UTF8);
            csvWriter.WriteLine(NamTableRow.Columns);
            string fileName = null;
            foreach(var row in rowList)
            {
                if (fileName == null)
                {
                    fileName = row.PartitionKey + ".csv";
                }
                csvWriter.WriteLine(row.ToString());
            }
            csvWriter.Flush();
            s.Position = 0;

            log.Info($"Completed csv creation--attempting to upload to ADLS");
          
            try
            {
                _adlsFileSystemClient.FileSystem.Create(_adlsAccountName, "/nam-csv-westus-v1/" + fileName, s, overwrite:true);
                log.Info($"Uploaded csv stream: {localFileName}");
            }
            catch (Exception e)
            {
                log.Info($"Upload failed: {e.Message}");
            }

            //delete local temp file
            File.Delete(localFileName);

            DateTime date = DateTime.ParseExact(name.Split('.')[0], "yyyyMMdd", null);
            return new FileProcessedTracker { ForecastDate = date, PartitionKey = "nam-grib-westus-v1", RowKey = name, Url = "unknown" };
        }
    }
}
