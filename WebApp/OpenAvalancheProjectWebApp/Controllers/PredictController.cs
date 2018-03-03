using OpenAvalancheProjectWebApp.Domain;
using OpenAvalancheProjectWebApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace OpenAvalancheProjectWebApp.Controllers
{
    public class PredictController : ApiController
    {
        private IForecastRepository repository;

        //TODO: migrate this to a dependency injection implementation
        public PredictController()
        {
            this.repository = new AzureTableForecastRepository();
        }

        /// <summary>
        /// Kick off making a prediction for the date, assumes the features file exists in blob storage
        /// This is a temporary measures to allow us to predict in an x64 app since 
        /// The other operatinal deployment weren't working correctly 
        /// 
        /// called as /api/Predict/20180201 --for Feb 2, 2018
        /// </summary>
        /// <param name="id">prediction date string format yyyyMMdd</param>
        /// <returns>Always returns Ok</returns>
        [HttpGet]
        public IHttpActionResult MakePrediction(string id)
        {
            PredictionUtilities.MakePredictions(this.repository, id);
            return Ok(); 
        }
    }
}
