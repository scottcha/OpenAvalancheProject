using OpenAvalancheProjectWebApp.Entities;
using OpenAvalancheProjectWebApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpenAvalancheProjectWebApp.Models
{
    public class ForecastViewModel
    {

        public ForecastViewModel(Forecast forecast)
        {
            ForecastForView = forecast;
            ForecastModelId = forecast.ForecastModelId; 
        }
        public string ForecastModelId { get; set; }
        //TODO: replace this with a lookup
        public static IEnumerable<ForecastModel> ForecastModels = new List<ForecastModel>
        {
            new Entities.ForecastModel
            {
                ForecastModelId = Constants.ModelDangerAboveTreelineV1,
                Name = Constants.ModelDangerAboveTreelineV1DisplayName
            },
            new Entities.ForecastModel
            {
                ForecastModelId = Constants.ModelDangerNearTreelineV1,
                Name = Constants.ModelDangerNearTreelineV1DisplayName
            },
            new Entities.ForecastModel
            {
                ForecastModelId = Constants.ModelDangerBelowTreelineV1,
                Name = Constants.ModelDangerBelowTreelineV1DisplayName
            }
        };
        public Forecast ForecastForView { get; set; }
    }
}