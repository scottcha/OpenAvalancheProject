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

            ////look back eight days and fill in any missing values; I beleive they store files on this server for 7 days
            //TableQuery<FileProcessedTracker> dateQuery = new TableQuery<FileProcessedTracker>().Where(
            //    TableQuery.CombineFilters(
            //        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName),
            //        TableOperators.And,
            //        TableQuery.GenerateFilterConditionForDate("ForecastDate", QueryComparisons.GreaterThan, DateTime.UtcNow.AddDays(-8))
            //    )
            //);

            //var results = table.ExecuteQuery(dateQuery);
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