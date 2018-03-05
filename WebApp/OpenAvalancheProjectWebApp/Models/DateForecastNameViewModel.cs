using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OpenAvalancheProjectWebApp.Models
{
    public class DateForecastNameViewModel
    {
        public DateForecastNameViewModel(string date, string modelId)
        {
            Date = date;
            ModelId = modelId;
        }
        public string Date { get; set; }
        public string ModelId { get; set; }
    }
}