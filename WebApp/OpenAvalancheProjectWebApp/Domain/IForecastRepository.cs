using OpenAvalancheProjectWebApp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace OpenAvalancheProjectWebApp.Domain
{
    public interface IForecastRepository
    {
        IQueryable<ForecastPoint> ForecastPoints { get; }
        void SaveForecast(Forecast forecast);
        
    }
}