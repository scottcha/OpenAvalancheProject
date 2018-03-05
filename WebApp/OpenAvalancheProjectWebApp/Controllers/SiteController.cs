using OpenAvalancheProjectWebApp.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace OpenAvalancheProjectWebApp.Controllers
{
    public class SiteController : ApiController
    {
        ////TODO: this doesn't seem to work, figure this out
        //[HttpGet]
        //public IHttpActionResult ClearCache()
        //{
        //    HttpResponse.RemoveOutputCacheItem("/WestUSv1/Index/");
        //    HttpResponse.RemoveOutputCacheItem("/Navigation/DateList/");
        //    return Ok();
        //}

        //TODO: utility to fix dates; delete once we are sure we don't need it
        //[HttpGet]
        //public IHttpActionResult FixDates()
        //{
        //    DateTime d = new DateTime(2018, 02, 01);
        //    DateTime dEnd = new DateTime(2018, 02, 27);
        //    AzureTableForecastRepository db = new AzureTableForecastRepository();
        //    while (d <= dEnd)
        //    {
        //        db.SaveForecastDate(new Entities.ForecastDate(d));
        //        d = d.AddDays(1);
        //    }
        //    return Ok();
        //}
    }
}
