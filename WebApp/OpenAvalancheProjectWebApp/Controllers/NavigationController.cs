using OpenAvalancheProjectWebApp.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OpenAvalancheProjectWebApp.Utilities;

namespace OpenAvalancheProjectWebApp.Controllers
{
    public class NavigationController : Controller
    {
        private IForecastRepository repository;

        //TODO: migrate this to a dependency injection implementation
        public NavigationController()
        {
            this.repository = new AzureTableForecastRepository();
        }

        public PartialViewResult ModelList(string modelId = null)
        {
            ViewBag.SelectedModelId = modelId;
            //TODO: dynamicall load these

            //IEnumerable<string> regions = repository.ModelIds
            //                                .Select(x => x.ModelId)
            //                                .Distinct()
            //                                .OrderBy(x => x);

            IEnumerable<string> modelIds = new List<string>(){
                                                                Constants.ModelDangerAboveTreelineV1,
                                                                Constants.ModelDangerNearTreelineV1,
                                                                Constants.ModelDangerBelowTreelineV1,
                                                                Constants.ModelDangerAboveTreelineV1NW,
                                                                Constants.ModelDangerNearTreelineV1NW,
                                                                Constants.ModelDangerBelowTreelineV1NW
                                                            };

            return PartialView(modelIds);
        }

        [OutputCache(Duration =3600, VaryByParam ="date")]
        public PartialViewResult DateList(DateTime? date = null)
        {
            if(date != null)
            {
                ViewBag.SelectedDate = date.Value.ToString("yyyyMMdd");
            }
            IEnumerable<DateTime> dates = repository.ForecastPoints
                                                .Select(p => p.Date).ToList();
            //below isn't supported against table storage so drop in to list then do this
            dates = dates.Distinct().OrderByDescending(x => x);

            List<string> dateStrings = new List<string>();
            foreach(var d in dates)
            {
                dateStrings.Add(d.ToString("yyyyMMdd"));
            }
            return PartialView(dateStrings);
        }
    }
}