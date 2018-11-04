using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpenAvalancheProjectWebApp.Utilities
{
    public static class Constants
    {
        //Table info
        public const string ForecastTableName = "forecastsuswestv1";
        public const string ForecastDatesTableName = "forecastsdatesuswestv1";
        public const string ForecastDatesParitionKey = "datespartition";

        //model info
        public const string ModelDangerAboveTreelineV1 = "DangerAboveTreelineV1";
        public const string ModelDangerNearTreelineV1 = "DangerNearTreelineV1";
        public const string ModelDangerBelowTreelineV1 = "DangerBelowTreelineV1";
        public const string ModelDangerAboveTreelineV1DisplayName = "Danger Above Treeline West US--Trained NWAC (V1)";
        public const string ModelDangerNearTreelineV1DisplayName = "Danger Near Treeline West US--Trained NWAC (V1)";
        public const string ModelDangerBelowTreelineV1DisplayName = "Danger Below Treeline West US--Trained NWAC (V1)";
        public const string ModelDangerAboveTreelineV1EvaluationImage = null;
        public const string ModelDangerNearTreelineV1EvaluationImage = null;
        public const string ModelDangerBelowTreelineV1EvaluationImage = null;

        public const string ModelDangerAboveTreelineV1NW = "DangerAboveTreelineV1NW";
        public const string ModelDangerNearTreelineV1NW= "DangerNearTreelineV1NW";
        public const string ModelDangerBelowTreelineV1NW = "DangerBelowTreelineV1NW";
        public const string ModelDangerAboveTreelineV1NWDisplayName = "Danger Above Treeline Northwest US--Trained NWAC (V1)";
        public const string ModelDangerNearTreelineV1NWDisplayName = "Danger Near Treeline Northwest US--Trained NWAC (V1)";
        public const string ModelDangerBelowTreelineV1NWDisplayName = "Danger Below Treeline Northwest US--Trained NWAC (V1)";
        public const string ModelDangerAboveTreelineV1NWEvaluationImage = "/Content/Images/DangerAboveTreelineV1NWAccuracy.jpg";
        public const string ModelDangerNearTreelineV1NWEvaluationImage = "/Content/Images/DangerNearTreelineV1NWAccuracy.jpg";
        public const string ModelDangerBelowTreelineV1NWEvaluationImage = "/Content/Images/DangerBelowTreelineV1NWAccuracy.jpg";

        //website strings
        public const string TrainingWarning = "Warning: Data outside Washington and Oregon has not been evaluated for accuracy and it can be extremely inaccurate. Do not use it for anything!  It is only being provided here to demonstrate the generalization of the machine learning prediction.  We are working on collecting the data from other regions to train and evaluate future models.";
        public const string TrainingWarningInaccurateModel = "Warning: Models/Data deployed for the 17-18 season had poor forcast accuracy. We are working on both improved models and improved accuracy evaluation for the 18-19 season.";
    }
}