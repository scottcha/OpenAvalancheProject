using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpenAvalancheProjectWebApp.Entities
{
    public class ForecastPoint : TableEntity
    {
        public ForecastPoint()
        { }

        public ForecastPoint(DateTime date, string modelName, float lat, float lon, string prediction)
        {
            Date = date;
            ModelName = modelName;
            Lat = lat;
            Lon = lon;
            Prediction = prediction;
            switch (prediction)
            {
                case "Low":
                    PredictionValue = 1;
                    break;
                case "Moderate":
                    PredictionValue = 2;
                    break;
                case "Considerable":
                    PredictionValue = 3;
                    break;
                case "High":
                    PredictionValue = 4;
                    break;
                case "Extreme":
                    PredictionValue = 5;
                    break;
                default:
                    PredictionValue = 0;
                    break;
            }

            PartitionKey = GeneratePartitionKey(date, modelName);
            RowKey = GenerateRowKey(lat, lon);
        }
        public DateTime Date { get; set; }
        public string ModelName { get; set; }
        private float lat;
        //TODO: figure out why table storage isn't adding lat/lon as their own columns
        public float Lat
        {
            get
            {
                if (lat == 0)
                    return float.Parse(RowKey.Split(':')[0]);
                else
                    return lat;
            }
            set
            {
                lat = value;
            }
        }
        private float lon;
        public float Lon
        {
            get
            {
                if (lon == 0)
                    return float.Parse(RowKey.Split(':')[1]);
                else
                    return lon;
            }
            set
            {
                lon = value;
            }
        }

        public string Prediction { get; set; }
        public int PredictionValue { get; set; }
        public static string GenerateRowKey(float Lat, float Lon)
        {
            return String.Format("{0}:{1}", Lat, Lon);
        }

        public static string GeneratePartitionKey(DateTime date, string modelName)
        {
            return date.ToString("yyyyMMdd") + modelName;
        }
    }
}