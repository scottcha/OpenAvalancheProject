using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OpenAvalancheProjectWebApp.Controllers
{
    public class MapController : Controller
    {
        //public PartialViewResult Map(int routeId)
        public PartialViewResult Map()
        {
            //return PartialView(repository.Routes.Where(r => r.RouteId == routeId).First());
            return PartialView();
        }
    }
}