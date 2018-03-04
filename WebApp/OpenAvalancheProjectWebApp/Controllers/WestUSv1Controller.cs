using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using XGBoost;
using OpenAvalancheProjectWebApp.Utilities;
using OpenAvalancheProjectWebApp.Models;
using OpenAvalancheProjectWebApp.Entities;
using OpenAvalancheProjectWebApp.Domain;

namespace OpenAvalancheProjectWebApp.Controllers
{
    public class WestUSv1Controller : Controller
    {
        private IForecastRepository repository;
       
        //TODO: migrate this to a dependency injection implementation
        public WestUSv1Controller()
        {
            this.repository = new AzureTableForecastRepository();
        }

        // GET: WestUSv1
        //cache for 1 hour--if a new forecast is generated we want it to be picked up within an hour
#if DEBUG != true
        [OutputCache(Duration = 3600, VaryByParam = "*")]
#endif
        public ActionResult Index(string date, string modelId = Constants.ModelDangerAboveTreelineV1NW, string region = "NWAC")
        {
            DateTime dateOfForecast = DateTime.UtcNow;
            if (date != null)
            {
                dateOfForecast = DateTime.ParseExact(date, "yyyyMMdd", null);
            }

            var forecastPoints = repository.ForecastPoints;

            //Check that we have a forecast for that date, if now get the most recent one before that
            var dateResult = forecastPoints.Where(p => p.PartitionKey == ForecastPoint.GeneratePartitionKey(dateOfForecast, modelId)).Select(p => p.Date);
            DateTime dateToQuery = dateOfForecast;
            if(dateResult.ToList().Count() == 0)
            {
                var dateResult2 = forecastPoints.Select(p => p.Date).ToList().OrderByDescending(d => d).First();
                dateToQuery = dateResult2; 
            }

            var result = forecastPoints.Where(p => p.PartitionKey == ForecastPoint.GeneratePartitionKey(dateToQuery, modelId)).ToList();
            if (result.Count > 0)
            {
                return View(new ForecastViewModel(new Forecast(result)));
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}