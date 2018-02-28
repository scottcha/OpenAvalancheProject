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

        //TODO: Convert this to a webapi which we can call from a function on a timer
        public ActionResult MakePrediction(DateTime? dateOfPrediction)
        {
            PredictionUtilities.MakePredictions(this.repository, dateOfPrediction);
            return RedirectToAction("Home", "Index");
        }

        // GET: WestUSv1
        public ActionResult Index()
        {
            var forecastPoints = repository.ForecastPoints;
            //look back eight days and fill in any missing values; I beleive they store files on this server for 7 days
            //TableQuery<FileProcessedTracker> dateQuery = new TableQuery<FileProcessedTracker>().Where(
            //    TableQuery.CombineFilters(
            //        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName),
            //        TableOperators.And,
            //        TableQuery.GenerateFilterConditionForDate("ForecastDate", QueryComparisons.GreaterThan, DateTime.UtcNow.AddDays(-8))
            //    )
            //);

            //var results = table.ExecuteQuery(dateQuery);

            var result = forecastPoints.Where(p => p.PartitionKey == ForecastPoint.GeneratePartitionKey(new DateTime(2017, 12, 31), Constants.ModelDangerAboveTreelineV1)).ToList();

            return View(new Forecast(result));
        }
    }
}