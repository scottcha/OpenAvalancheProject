/*
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
using OpenAvalancheProject.Pipeline.Utilities;

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
            GribUtilities.TryFindBootstrapLibrary(out attemptPath);
            log.Info($"Attemping to find lib: {attemptPath}");
#if DEBUG == false
            GribEnvironment.DefinitionsPath = @"D:\home\site\wwwroot\bin\Grib.Api\definitions";
#endif
            GribEnvironment.Init();

            //1. Download stream to temp
            //TODO: there is supposedly now an ability to read a stream direction in GRIBAPI.Net; investigate to see if its better than storing a temp file
            string localFileName = AzureUtilities.DownloadBlobToTemp(myBlob, name, log);

            var rowList = new List<NamTableRow>();

            //2. Get values from file
            using (GribFile file = new GribFile(localFileName))
            {
                log.Info($"Parsing file {name}");
                rowList = GribUtilities.ParseNamGribFile(file);
            }

            //3. Format in correct table format
            DataLakeStoreFileSystemManagementClient adlsFileSystemClient;
            string adlsAccountName;
            AzureUtilities.AuthenticateADLSFileSystemClient(out adlsFileSystemClient, out adlsAccountName, log);

            try
            {
                adlsFileSystemClient.FileSystem.UploadFile(adlsAccountName, localFileName, "/nam-grib-westus-v1/" + name, uploadAsBinary: true, overwrite: true);
                log.Info($"Uploaded file: {localFileName}");
            }
            catch (Exception e)
            {
                log.Error($"Upload failed: {e.Message}");
            }

            MemoryStream s = new MemoryStream();
            StreamWriter csvWriter = new StreamWriter(s, Encoding.UTF8);
            csvWriter.WriteLine(NamTableRow.Columns);
            string fileName = null;
            foreach (var row in rowList)
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
                adlsFileSystemClient.FileSystem.Create(adlsAccountName, "/nam-csv-westus-v1/" + fileName, s, overwrite: true);
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
*/