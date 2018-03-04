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
        public AzureTableDbContext()
        {
            CloudTableClient tableClient = AzureUtilities.CloudTableClient;
            Table = tableClient.GetTableReference(Constants.ForecastTableName);
            Table.CreateIfNotExists();
        }

        public IQueryable<ForecastPoint> ForecastPoints
        {
            get
            {
                var q = this.Table.CreateQuery<ForecastPoint>();
                return q;
            }
        }
    }
}