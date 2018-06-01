using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace GetNWACData
{
    public class AvalancheRegionForecast
    {
        public string Zone{get;set;}
        public DateTime PublishDate{get;set;}
        public string ResourceUri{get;set;}
        public string SpecialStatement{get;set;}
        public DateTime Day1Date;
        public string BottomLineSummary{get;set;}
        public string Day1DangerElevationHigh{get;set;}
        public string Day1DangerElevationMiddle{get;set;}
        public string Day1DangerElevationLow{get;set;}
        public string Day1DetailedForecast{get;set;}
        public string Day1Trend{get;set;}
        public string Day1Warning{get;set;}
        public DateTime? Day1WarningEnd{get;set;}
        public string Day1WarningText{get;set;}
        public string Day2DangerElevationHigh{get;set;}
        public string Day2DangerElevationMiddle{get;set;}
        public string Day2DangerElevationLow{get;set;}
        public string Day2DetailedForecast{get;set;}
        public string Day2Trend{get;set;}
        public string Day2Warning{get;set;}
        public DateTime? Day2WarningEnd{get;set;}
        public string Day2WarningText{get;set;}

        public List<AvalancheProblem> AvalancheProblems{get;set;}
        public string ToString(out StringBuilder sbHeader)
        {
            sbHeader = new StringBuilder();
            sbHeader.Append("Region, PublishedDateTime, Day1Date, SpecialStatement, BottomLineSummary, ForecastUrl, ");
            sbHeader.Append("Day1DangerAboveTreeline, Day1DangerNearTreeline, Day1DangerBelowTreeline, Day1DetailedForecast, Day1Warning, Day1WarningEnd, Day1WarningText, ");
            sbHeader.Append("Day2DangerAboveTreeline, Day2DangerNearTreeline, Day2DangerBelowTreeline, Day2DetailedForecast, Day2Warning, Day2WarningEnd, Day2WarningText, ");

            var sbBody = new StringBuilder();
            sbBody.Append(Zone + ",");
            sbBody.Append(PublishDate.ToUniversalTime().ToString("yyyyMMdd HH:00" + ","));
            sbBody.Append(Day1Date.ToUniversalTime().ToString("yyyyMMdd") + ",");
            sbBody.Append(Utilities.CleanStringForCSVExport(SpecialStatement) + ",");  //replace commas and quotes & \n since we are exporting to csv
            sbBody.Append(Utilities.CleanStringForCSVExport(BottomLineSummary) + ",");
            sbBody.Append(ResourceUri + ",");
            sbBody.Append(Day1DangerElevationHigh + ",");
            sbBody.Append(Day1DangerElevationMiddle + ",");
            sbBody.Append(Day1DangerElevationLow + ",");
            sbBody.Append(Utilities.CleanStringForCSVExport(Day1DetailedForecast) + ",");
            sbBody.Append(Utilities.CleanStringForCSVExport(Day1Warning) + ",");
            sbBody.Append((Day1WarningEnd.HasValue ? Day1WarningEnd.Value.ToUniversalTime().ToString("yyyyMMdd HH:00") + "," : DateTime.MinValue.ToString("yyyyMMdd HH:00") + ","));
            sbBody.Append(Utilities.CleanStringForCSVExport(Day1WarningText) + ",");
            sbBody.Append(Day2DangerElevationHigh + ",");
            sbBody.Append(Day2DangerElevationMiddle + ",");
            sbBody.Append(Day2DangerElevationLow + ",");
            sbBody.Append(Utilities.CleanStringForCSVExport(Day2DetailedForecast) + ",");
            sbBody.Append(Utilities.CleanStringForCSVExport(Day2Warning) + ",");
            sbBody.Append((Day2WarningEnd.HasValue ? Day2WarningEnd.Value.ToUniversalTime().ToString("yyyyMMdd HH:00") + "," : DateTime.MinValue.ToString("yyyyMMdd HH:00") + ","));
            sbBody.Append(Utilities.CleanStringForCSVExport(Day2WarningText) + ",");

            ExtractAvalancheProblem("Cornices", sbHeader, sbBody);
            ExtractAvalancheProblem("Glide", sbHeader, sbBody);
            ExtractAvalancheProblem("Loose Dry", sbHeader, sbBody);
            ExtractAvalancheProblem("Loose Wet", sbHeader, sbBody);
            ExtractAvalancheProblem("Persistent Slab", sbHeader, sbBody);
            ExtractAvalancheProblem("Deep Persistent Slab", sbHeader, sbBody); //TODO: need to check this is actually what NWAC uses
            ExtractAvalancheProblem("Storm Slabs", sbHeader, sbBody);
            ExtractAvalancheProblem("Wet Slabs", sbHeader, sbBody);
            ExtractAvalancheProblem("Wind Slab", sbHeader, sbBody);
           
            return sbBody.ToString();
        }

        private void ExtractAvalancheProblem(string problemName, StringBuilder sbHeader, StringBuilder sbBody)
        {
            var problem  = AvalancheProblems.Where(p => p.ProblemName == problemName);
            if (problem .Count() == 1)
            {
                sbHeader.Append(problem .First().Header());
                sbBody.Append(problem .First().ToString());
            }
            else
            {
                var p = new AvalancheProblem()
                {
                    ProblemName = problemName,
                    Likelihood = "no-data",
                    MaximumSize = "no-data",
                    MinimumSize = "no-data"
                };
                sbHeader.Append(p.Header());
                sbBody.Append(p.ToString());
            }
        }
    }
}