using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.IO;
using System.Net.Http;

namespace OpenAvalancheProject.Pipeline.Functions
{
    public static class DownloadFromQueueToBlob
    {
        //create a shared httpclient so as not to exhaust resources
        //https://docs.microsoft.com/en-us/azure/architecture/antipatterns/improper-instantiation/
        private static readonly HttpClient client;
        static DownloadFromQueueToBlob()
        {
            client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
        }
        
        [FunctionName("DownloadFromQueueToBlob")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run([QueueTrigger("filereadytodownloadqueue", Connection = "AzureWebJobsStorage")]FileReadyToDownloadQueueMessage myQueueItem,
                          [Blob("{FileType}/{UniqueFileName}", FileAccess.Write)] Stream myOutputBlob,
                          TraceWriter log)
        {
            string partitionName = myQueueItem.Filetype; 

            log.Info($"C# DownloadFromQueueToBlob queue trigger function processing: {myQueueItem.UniqueFileName}");

            var urlToDownload = myQueueItem.Url; 
            log.Info($"Downloading Url {urlToDownload}");
         
            client.GetByteArrayAsync(urlToDownload).ContinueWith(
                (requestTask) =>
                {
                    try
                    {
                        byte[] buffer = requestTask.Result;
                        myOutputBlob.Write(buffer, 0, buffer.Length);
                        myOutputBlob.Flush();
                        myOutputBlob.Close();
                    }
                    catch(System.AggregateException e)
                    {
                        log.Error($"Got exception {e.ToString()} when processing file {myQueueItem.UniqueFileName}");
                    }
                }
            );
        }
    }
}