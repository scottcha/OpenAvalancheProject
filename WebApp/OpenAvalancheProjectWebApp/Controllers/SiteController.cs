using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace OpenAvalancheProjectWebApp.Controllers
{
    //TODO: this doesn't seem to work, figure this out
    public class SiteController : ApiController
    {
        [HttpGet]
        public IHttpActionResult ClearCache()
        {
            HttpResponse.RemoveOutputCacheItem("/WestUSv1/Index/");
            HttpResponse.RemoveOutputCacheItem("/Navigation/DateList/");
            return Ok();
        }

    }
}
