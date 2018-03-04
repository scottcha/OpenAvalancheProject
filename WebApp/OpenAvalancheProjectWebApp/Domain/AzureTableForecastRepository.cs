using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using OpenAvalancheProjectWebApp.Entities;
using System.Diagnostics;

namespace OpenAvalancheProjectWebApp.Domain
{
    public class AzureTableForecastRepository : IForecastRepository
    {
        private AzureTableDbContext context = new AzureTableDbContext();

        IQueryable<ForecastPoint> IForecastRepository.ForecastPoints => context.ForecastPoints;

        public void SaveForecast(Forecast forecast)
        {
            var op = new TableBatchOperation();
            for(int i =0; i < forecast.ForecastPoints.Count; i++)
            {
                var table = context.Table;
                op.InsertOrMerge(forecast.ForecastPoints[i]);
                if((i+1) % 100 == 0)
                {
                    var result = table.ExecuteBatch(op);
                    op = new TableBatchOperation();
                }
            }
        }    
    }
}