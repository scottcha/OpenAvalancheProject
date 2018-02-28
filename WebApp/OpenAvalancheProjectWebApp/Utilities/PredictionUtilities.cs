using Microsoft.WindowsAzure.Storage.Blob;
using OpenAvalancheProjectWebApp.Domain;
using OpenAvalancheProjectWebApp.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using XGBoost;

namespace OpenAvalancheProjectWebApp.Utilities
{
    public class PredictionUtilities
    {
        private static Tuple<float[][], float[][]> CreatePredictionFormat(string fileName)
        {
            List<List<float>> cache = new List<List<float>>();
            List<List<float>> latLons = new List<List<float>>();
            var lines = System.IO.File.ReadAllLines(fileName).Select(a => a.Split(','));
            bool firstRow = true;
            foreach (var line in lines)
            {
                if (firstRow)
                {
                    firstRow = false;
                    continue;
                }
                var rowList = new List<float>();
                var latLonList = new List<float>();

                latLonList.Add(float.Parse(line[1])); //lat
                latLonList.Add(float.Parse(line[2])); //lon
                //3 skips date, lat and lon
                for (int i = 3; i < line.Length; i++)
                {
                    //if no value then 0
                    float val = 0;
                    float.TryParse(line[i], out val);
                    //-9999 indicates missing, set to 0 for prediction
                    if(val == -9999)
                    {
                        val = 0;
                    }
                    rowList.Add(val);
                }
                cache.Add(rowList);
                latLons.Add(latLonList);
            }

            //convert to array of floats
            float[][] values = new float[cache.Count][];
            for (int i = 0; i < cache.Count; i++)
            {
                values[i] = cache[i].ToArray();
            }

            float[][] latLonValues = new float[latLons.Count][];
            for(int i = 0; i < latLons.Count; i++)
            {
                latLonValues[i] = latLons[i].ToArray();
            }
            return new Tuple<float[][], float[][]>(latLonValues, values);
        }

        private static List<string> PredictAboveDangerV1(float[][] values)
        {
            //deploy resource command should place is in working dir
            CloudBlobContainer container = AzureUtilities.ModelBlobContainer;
            CloudBlockBlob blob = container.GetBlockBlobReference("ModelAboveV1.bin");

            string localFileName = String.Empty;
            using (var fileStream = System.IO.File.OpenWrite(Path.GetTempFileName()))
            {
                blob.DownloadToStream(fileStream);
                localFileName = fileStream.Name;
            }
            if(localFileName == String.Empty)
            {
                throw new FileNotFoundException("Could not find model file: ModelAboveV1.bin before predicting");
            }
            var xgbc = BaseXgbModel.LoadClassifierFromFile(localFileName);
            //format is single column with one row for each probability for that class; need to decode it
            var predictions = xgbc.PredictRaw(values);
            var decodedPredictions = DecodePredictionsAboveDangerV1(predictions);
            return decodedPredictions;
        }

        /// <summary>
        /// Decode the single column of probabilties to class predictions
        /// Warning: this is based on the current trained model and may change with a new model
        /// </summary>
        /// <param name="predictions">single column of class prediction probabilties returned from xgboost predict</param>
        /// <returns></returns>
        private static List<string> DecodePredictionsAboveDangerV1(float[] predictions)
        {
            //encoding of current v1 above model
            //first value: considerable
            //second value: high
            //third value: low
            //fourth value: moderate
            if(predictions.Length % 4 != 0)
            {
                throw new ArgumentException("Expected predictions to be divisible by 4 since we have 4 classes we are predicting");
            }

            var calculatedPredictions = new List<string>();
            int i = 0;
            while(i < predictions.Length)
            {
                var row = new List<float>()
                {
                    predictions[i++],
                    predictions[i++],
                    predictions[i++],
                    predictions[i++]
                };
                var index = row.IndexOf(row.Max());
                switch(index)
                {
                    case 0:
                        calculatedPredictions.Add("Considerable");
                        break;
                    case 1:
                        calculatedPredictions.Add("High");
                        break;
                    case 2:
                        calculatedPredictions.Add("Low");
                        break;
                    case 3:
                        calculatedPredictions.Add("Moderate");
                        break;
                    default:
                        throw new Exception("Unexpected: Have more than four values in decode. ");

                }
            }
            return calculatedPredictions;
        }

        public static Forecast MakePredictions(IForecastRepository db, DateTime? dateOfForecast)
        {
            //DateTime date = dateOfForecast ?? DateTime.UtcNow;
            DateTime date = dateOfForecast ?? new DateTime(2017, 12, 31);

            CloudBlobContainer container = AzureUtilities.FeaturesBlobContainer;
            string cloudFileName = "V1Features" + date.ToString("yyyyMMdd") + ".csv";
            CloudBlockBlob blob = container.GetBlockBlobReference(cloudFileName);

            string localFileName = String.Empty;
            using (var fileStream = System.IO.File.OpenWrite(Path.GetTempFileName()))
            {
                blob.DownloadToStream(fileStream);
                localFileName = fileStream.Name;
            }

            var values = PredictionUtilities.CreatePredictionFormat(localFileName);
            var latLons = values.Item1;
            var predictions = PredictionUtilities.PredictAboveDangerV1(values.Item2);

            if (latLons.Length != predictions.Count())
            {
                throw new Exception("Predictions don't have the same lenth as LatLon");
            }
            var mappedPredictions = new List<ForecastPoint>();
            for (int i = 0; i < latLons.Length; i++)
            {
                mappedPredictions.Add(new ForecastPoint(date, Constants.ModelDangerAboveTreelineV1, latLons[i][0], latLons[i][1], predictions[i]));
            }

            //upload the predictions to table storage
            var forecast = new Forecast(mappedPredictions);
            db.SaveForecast(forecast);
            return forecast;
        }
    }
}