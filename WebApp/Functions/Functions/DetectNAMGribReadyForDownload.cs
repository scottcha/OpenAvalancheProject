using System;
using System.Net;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using OpenAvalancheProject.Pipeline.Utilities;

namespace OpenAvalancheProject.Pipeline.Functions
{
    [StorageAccount("AzureWebJobsStorage")]
    public static class DetectNAMGribReadyForDownload
    {
        //Execute every three hours starting 3am (approximate time t00 forcast is fully available) times are UTC
        [FunctionName("DetectNAMGribReadyForDownload")]
        [return: Queue("filereadytodownloadqueue")]
        public static void Run([TimerTrigger("0 0 3/3 1/1 * *", RunOnStartup = true), Disable()]TimerInfo myTimer, 
                               [Queue("filereadytodownloadqueue", Connection = "AzureWebJobsStorage")] ICollector<FileReadyToDownloadQueueMessage> outputQueueItem, 
                               TraceWriter log)
        {
            string partitionName = Constants.NamTrackerPartitionKey;
            log.Info($"DetectNAMGribReadyForDownload Timer trigger function executed at UTC: {DateTime.UtcNow}");
            
            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebJobsStorage"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(Constants.NamTrackerTable);
            table.CreateIfNotExists();

            //look back eight days and fill in any missing values; I beleive they store files on this server for 7 days
            TableQuery<FileProcessedTracker> dateQuery = new TableQuery<FileProcessedTracker>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate("ForecastDate", QueryComparisons.GreaterThan, DateTime.UtcNow.AddDays(-8))
                )
            );

            var results = table.ExecuteQuery(dateQuery);

            //find the list of files available on the server
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(@"http://nomads.ncep.noaa.gov/pub/data/nccf/com/nam/prod/");
            string dateResponseString = "";
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                dateResponseString = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
            }
            //find the dates available in that string
            //>nam.(\d+) matches the date strings
            Regex regex = new Regex(@">nam.(\d+)");
            MatchCollection matches = regex.Matches(dateResponseString);
            List<string> dateList = new List<string>();
            string dateListLogString = "";
            foreach(Match match in matches)
            {
                dateList.Add(match.Groups[1].Value);
                dateListLogString += match.Groups[1].Value;
            }
            log.Info($"Have list of nam directories: {dateListLogString}");
            //for each date list get the file list
            string fileResponseString = "";
            var fileList = new List<Tuple<string, string>>();
#if DEBUG == true
            //shorten list for debugging 
            dateList = dateList.GetRange(0, 1);
#endif
            foreach (var dateString in dateList)
            {
                HttpWebRequest requestInner = (HttpWebRequest)HttpWebRequest.Create(@"http://nomads.ncep.noaa.gov/pub/data/nccf/com/nam/prod/nam." + dateString);
                using (HttpWebResponse response = (HttpWebResponse)requestInner.GetResponse())
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    fileResponseString = reader.ReadToEnd();
                    reader.Close();
                    dataStream.Close();
                }
                //find the dates available in that string
                //only looking at the t00 forcast for now, in the future we can expand to the other forecast runs
                Regex regexFile = new Regex(@">(nam\.t00z\.awphys[\S]+)\.idx");
                MatchCollection matchesFile = regexFile.Matches(fileResponseString);
                foreach (Match match in matchesFile)
                {
                    fileList.Add(new Tuple<string, string>(match.Groups[1].Value, dateString));
                }
            }

            log.Info($"Have list of {fileList.Count} nam files to compare");
#if DEBUG == true
            //shorten list for debugging 
            fileList = fileList.GetRange(0, 1);
#endif
            //compare fileList to existing files
            foreach(var file in fileList)
            {
                int countOfFilesInTable = results.Where(f => f.RowKey == file.Item2 + "." + file.Item1 && f.PartitionKey == partitionName).Count();
                if (countOfFilesInTable == 1)
                {
                    //file exists already
                    log.Info($"Already have {file.Item1} with date {file.Item2} in table as downloaded.");
                    continue;
                }
                else if (countOfFilesInTable > 1)
                {
                    log.Error($"Have multiple files with name {file.Item1} in partition {partitionName}");
                }
                else
                {
                    //template is the for grib filter tool available here: http://nomads.ncep.noaa.gov/txt_descriptions/grib_filter_doc.shtml
                    //TODO: should make levels and variables configurable
                    //TODO: should make region configurable
                    string downloadTemplate = @"http://nomads.ncep.noaa.gov/cgi-bin/filter_nam.pl?file=%FILENAME%&lev_10_m_above_ground=on&lev_80_m_above_ground&lev_2_m_above_ground=on&lev_surface=on&lev_tropopause=on&var_APCP=on&var_CRAIN=on&var_CSNOW=on&var_RH=on&var_TMP=on&var_UGRD=on&var_VGRD=on&subregion=&leftlon=-125&rightlon=-104&toplat=49&bottomlat=32&dir=%2Fnam.%DATE%";
                    downloadTemplate = downloadTemplate.Replace("%FILENAME%", file.Item1);
                    downloadTemplate = downloadTemplate.Replace("%DATE%", file.Item2);
                    log.Info($"Adding file {file.Item1} with date {file.Item2} to download queue.");
                    //enter a new queue item for every file missing
                    outputQueueItem.Add(new FileReadyToDownloadQueueMessage{ FileName=file.Item1, FileDate=file.Item2, Url = downloadTemplate, Filetype = partitionName });
                }
            }
        }
    }
}