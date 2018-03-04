using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpenAvalancheProjectWebApp.Utilities
{
    public static class Constants
    {
        public const string ForecastTableName = "forecastsuswestv1";
        public const string ModelDangerAboveTreelineV1 = "DangerAboveTreelineV1";
        public const string ModelDangerNearTreelineV1 = "DangerNearTreelineV1";
        public const string ModelDangerBelowTreelineV1 = "DangerBelowTreelineV1";
<<<<<<< HEAD
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

        public const string TrainingWarning = "Warning: Data outside Washington and Oregon has not been evaluated for accuracy and it can be extremely inaccurate. Do not use it for anything!  It is only being provided here to demonstrate the generalization of the machine learning prediction.  We are working on collecting the data from other regions to train and evaluate future models.";
=======
        public const string ModelDangerAboveTreelineV1DisplayName = "Danger Above Treeline--Trained NWAC (V1)";
        public const string ModelDangerNearTreelineV1DisplayName = "Danger Near Treeline--Trained NWAC (V1)";
        public const string ModelDangerBelowTreelineV1DisplayName = "Danger Below Treeline--Trained NWAC (V1)";
>>>>>>> 74064c9d858efc5ab0d74cdebf17a912158f7e46
    }
}