using OpenAvalancheProjectWebApp.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OpenAvalancheProjectWebApp.Utilities;
using OpenAvalancheProjectWebApp.Models;

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
                                                                Constants.ModelDangerAboveTreelineV1NW,
                                                                Constants.ModelDangerNearTreelineV1NW,
                                                                Constants.ModelDangerBelowTreelineV1NW,
                                                                Constants.ModelDangerAboveTreelineV1,
                                                                Constants.ModelDangerNearTreelineV1,
                                                                Constants.ModelDangerBelowTreelineV1
                                                            };

            return PartialView(modelIds);
        }
#if DEBUG != true
        [OutputCache(Duration =3600, VaryByParam ="*")]
#endif
        public PartialViewResult DateList(DateTime? date = null, string modelId = null)
        {
            if(date != null)
            {
                ViewBag.SelectedDate = date.Value.ToString("yyyyMMdd");
            }
            IEnumerable<string> dates = repository.ForecastDates
                                                .Select(p => p.RowKey).ToList();
            //below isn't supported against table storage so drop in to list then do this
            dates = dates.OrderByDescending(x => x);

            List<DateForecastNameViewModel> viewModelList = new List<DateForecastNameViewModel>();
            foreach(var d in dates)
            {
                viewModelList.Add(new DateForecastNameViewModel(d, modelId));
            }
            return PartialView(viewModelList);
        }
    }
}