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
        public Forecast ForecastForView { get; set; }

    }
}