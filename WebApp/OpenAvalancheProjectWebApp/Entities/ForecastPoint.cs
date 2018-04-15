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

            //TODO: move this to dynamically updatable from a table or blob
            if (lat > 42.752975 && lat < 44.74359 && lon > -122.191917 && lon < -121.42812)
            {
                //this needs to be before cascades as they overlap
                RegionName = "Central Oregon";
            }
            else if (lat > 42.027657 && lat < 49.21027 && lon > -122.144597 && lon < -120.172867)
            {
                RegionName = "Cascades";
            }
            else if (lat > 47.328348 && lat < 48.133995 && lon > -124.289114 && lon < -123.012613)
            {
                RegionName = "Olympics";
            }
            else if (lat > 40.389966 && lat < 41.54341 && lon > -122.359943 && lon < -121.140982)
            {
                RegionName = "Shasta/Lassen";
            }
            else if (lat > 36.042061 && lat < 40.118984 && lon > -120.789127 && lon < -117.967179)
            {
                RegionName = "Sierra";
            }
            else if (lat > 35.183004 && lat < 35.449788 && lon > -111.862791 && lon < -111.323939)
            {
                RegionName = "Flagstaff";
            }
            else if (lat > 36.266822 && lat < 36.861803 && lon > -105.698237 && lon < -105.239173)
            {
                RegionName = "Taos";
            }
            else if (lat > 39.659701 && lat < 46.940989 && lon > -112.19668 && lon < -109.204224)
            {
                RegionName = "Montana/Utah/Wyoming";
            }
            else if (lat > 37.001006 && lat < 40.95222 && lon > -108.973951 && lon < -104.705104)
            {
                RegionName = "Colorado";
            }
            else if (lat > 43.50579 && lat < 48.984054 && lon > -117.82529 && lon < -112.59382)
            {
                RegionName = "Idaho";
            }
            else
            {
                RegionName = "Unknown";
            }

            PartitionKey = GeneratePartitionKey(date, modelName);
            RowKey = GenerateRowKey(lat, lon);
        }
        public DateTime Date { get; set; }
        public string ModelName { get; set; }
        public string RegionName { get; set; }
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