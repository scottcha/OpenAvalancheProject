using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace OAP
{
    public static class UnzipRegions
    {
        [FunctionName("UnzipRegions")]
        public static async Task Run([BlobTrigger("oapmldata/{name}", Connection = "pangeoaksstorage_STORAGE")]CloudBlockBlob myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name}");

            string destinationStorage = Environment.GetEnvironmentVariable("destinationStorage");
            string destinationContainer = Environment.GetEnvironmentVariable("destinationContainer");

            try{
                if(name.Split('.').Last().ToLower() == "zip"){

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(destinationStorage);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobClient.GetContainerReference(destinationContainer);


                    bool result = await container.CreateIfNotExistsAsync();
                    if(result)
                    {
                        log.LogInformation($"Created container {destinationContainer}");
                    }

                    using(MemoryStream blobMemStream = new MemoryStream()){

                        await myBlob.DownloadToStreamAsync(blobMemStream);

                        using(ZipArchive archive = new ZipArchive(blobMemStream))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                log.LogInformation($"Now processing {entry.FullName}");

                                string validName = entry.FullName;
                                log.LogInformation($"Writing to container {destinationContainer}");
                                log.LogInformation($"Writing to valid name {validName}");
                                CloudBlockBlob blockBlob = container.GetBlockBlobReference(validName);
                                using (var fileStream = entry.Open())
                                {
                                    await blockBlob.UploadFromStreamAsync(fileStream);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex){
                log.LogInformation($"Error! Something went wrong: {ex.Message}");

            }            
        }
    }
}
