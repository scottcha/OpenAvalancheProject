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
        IQueryable<ForecastDate> ForecastDates{ get; }
        void SaveForecast(Forecast forecast);
        void SaveForecastDate(ForecastDate date);
        void SaveForecastPoint(ForecastPoint point);
    }
}