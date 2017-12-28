using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace OpenAvalancheProject.Pipeline
{
    public class FileProcessedTracker : TableEntity
    {
        public DateTime ForecastDate { get; set; }
        public string Url { get; set; }
        public FileProcessedTracker(string fileType, string fileName, DateTime forecastDate, string url)
        {
            this.PartitionKey = fileType;
            this.RowKey = fileName;
            this.ForecastDate = forecastDate;
            this.Url = url;
        }
        public FileProcessedTracker() { }

    }
}
