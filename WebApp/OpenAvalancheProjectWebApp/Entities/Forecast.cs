using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;
using OpenAvalancheProjectWebApp.Utilities;

namespace OpenAvalancheProjectWebApp.Entities
{
    public class Forecast
    {
        public Forecast(List<ForecastPoint> forecastPoints)
        {
            ForecastPoints = forecastPoints;
            Date = ForecastPoints[0].Date;
        }

        public string ForecastModelId
        {
            get
            {
                return ForecastPoints[0].ModelName;
            }
        }

        public DateTime Date { get; set; }
        public List<ForecastPoint> ForecastPoints
        {
            get; set;
        }

        public string ToGeoJson()
        {
            var features = new List<Feature>();
            foreach (var p in ForecastPoints)
            {
                var geometry = new Point(new Position(p.Lat, p.Lon));
                var feature = new Feature(geometry, new Dictionary<string, object>()
                {
                    ["Date"] = p.Date,
                    ["Forecast"] = p.Prediction,
                    ["ModelName"] = p.ModelName
                });

                features.Add(feature);
            }

            return JsonConvert.SerializeObject(features);
        }

        public string LatArray
        {
            get
            {
                return JsonConvert.SerializeObject(ForecastPoints.Select(p => p.Lat).ToArray());
            }
        }

        public string LonArray
        {
            get
            {
                return JsonConvert.SerializeObject(ForecastPoints.Select(p => p.Lon).ToArray());
            }
        }

        public string ForecastValues
        {
            get
            {
                return JsonConvert.SerializeObject(ForecastPoints.Select(p => p.PredictionValue / 5.0f).ToArray());
            }
        }

        public string ForecastLabels
        {
            get
            {
                return JsonConvert.SerializeObject(ForecastPoints.Select(p => p.Prediction).ToArray());
            }
        }

        public string ForecastTitle
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                switch(ForecastPoints[0].ModelName)
                {
                    case Constants.ModelDangerAboveTreelineV1:
                        sb.Append(Constants.ModelDangerAboveTreelineV1DisplayName);
                        break;
                    case Constants.ModelDangerBelowTreelineV1:
                        sb.Append(Constants.ModelDangerBelowTreelineV1DisplayName);
                        break;
                    case Constants.ModelDangerNearTreelineV1:
                        sb.Append(Constants.ModelDangerNearTreelineV1DisplayName);
                        break;
                    case Constants.ModelDangerAboveTreelineV1NW:
                        sb.Append(Constants.ModelDangerAboveTreelineV1NWDisplayName);
                        break;
                    case Constants.ModelDangerBelowTreelineV1NW:
                        sb.Append(Constants.ModelDangerBelowTreelineV1NWDisplayName);
                        break;
                    case Constants.ModelDangerNearTreelineV1NW:
                        sb.Append(Constants.ModelDangerNearTreelineV1NWDisplayName);
                        break;
                    default:
                        sb.Append("Unknown Model");
                        break;
                }
                sb.Append(" ").Append(Date.ToShortDateString());
                return sb.ToString();
            }
        }

        public string ForecastAccuracyImagePath
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                switch (ForecastPoints[0].ModelName)
                {
                    case Constants.ModelDangerAboveTreelineV1:
                        sb.Append(Constants.ModelDangerAboveTreelineV1EvaluationImage);
                        break;
                    case Constants.ModelDangerBelowTreelineV1:
                        sb.Append(Constants.ModelDangerNearTreelineV1EvaluationImage);
                        break;
                    case Constants.ModelDangerNearTreelineV1:
                        sb.Append(Constants.ModelDangerBelowTreelineV1EvaluationImage);
                        break;
                    case Constants.ModelDangerAboveTreelineV1NW:
                        sb.Append(Constants.ModelDangerAboveTreelineV1NWEvaluationImage);
                        break;
                    case Constants.ModelDangerBelowTreelineV1NW:
                        sb.Append(Constants.ModelDangerNearTreelineV1NWEvaluationImage);
                        break;
                    case Constants.ModelDangerNearTreelineV1NW:
                        sb.Append(Constants.ModelDangerBelowTreelineV1NWEvaluationImage);
                        break;
                    default:
                        sb.Append(String.Empty);
                        break;
                }
                return sb.ToString();
            }
        }
    }
}