using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using OpenAvalancheProjectWebApp.Entities;
using OpenAvalancheProjectWebApp.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace OpenAvalancheProjectWebApp.Domain
{
    public class AzureTableDbContext : DbContext
    {
        public CloudTable Table { get; set; }
        public CloudTable ForecastDateTable { get; set; }
        public AzureTableDbContext()
        {
            CloudTableClient tableClient = AzureUtilities.CloudTableClient;
            Table = tableClient.GetTableReference(Constants.ForecastTableName);
            Table.CreateIfNotExists();

            ForecastDateTable = tableClient.GetTableReference(Constants.ForecastDatesTableName);
            ForecastDateTable.CreateIfNotExists();
        }

        public IQueryable<ForecastPoint> ForecastPoints
        {
            get
            {
                var q = this.Table.CreateQuery<ForecastPoint>();
                return q;
            }
        }

        public IQueryable<ForecastDate> ForecastDates
        {
            get
            {
                var q = this.ForecastDateTable.CreateQuery<ForecastDate>();
                return q;
            }
        }
    }
}