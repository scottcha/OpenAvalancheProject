using Microsoft.WindowsAzure.Storage.Table;
using OpenAvalancheProjectWebApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpenAvalancheProjectWebApp.Entities
{
    public class ForecastDate : TableEntity
    {
        public ForecastDate()
        { }

        public ForecastDate(DateTime date)
        {
            PartitionKey = Constants.ForecastDatesParitionKey;
            RowKey = date.ToString("yyyyMMdd");
        }
    }
}