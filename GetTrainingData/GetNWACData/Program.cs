using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text;

namespace GetNWACData
{
    /// <summary>
    /// console app to pull forecast history from NWAC api and export to csv
    /// Need to populate app and loc before using
    /// </summary>
    class Program
    {
        private static bool isFirst = true;
        static void Main(string[] args)
        {
            using (StreamWriter file = new StreamWriter(@"..\..\..\nwacforecasts.csv"))
            {
                //var years = new List<int>() { 2013, 2014, 2015, 2016, 2017, 2018, 2019, 2020 };
                var years = new List<int>() { 2018, 2019, 2020 };
                var months = new Dictionary<int, List<int>>()
                {
                    [2018] = new List<int>{11, 12},
                    [2019] = new List<int>{1, 2, 3, 4, 11, 12},
                    [2020] = new List<int>{1, 2, 3, 4}
                };
                foreach (var year in years)
                {
                    foreach (var month in months[year])
                    {
                        for (int day = 1; day <= 31; day++)
                        {
                            GetForecast(year, month, day, file);
                            System.Threading.Thread.Sleep(500); //pause for a 1/2 second so we don't overwhealm the server 
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Makes call to nwac api and then parses the json response
        /// </summary>
        /// <param name="uri">nwac api call</param>
        /// <returns>parsed list of avalanche forecasts one item in list per regional forecast</returns>
        private static async Task<List<AvalancheRegionForecast>> GetAsync(string uri)
        {   
            //Can debug using this instead of hitting the webserver
            //var response = await File.ReadAllTextAsync(@"../../../sampleforecast.json");
            var httpClient = new HttpClient();
            //nwac api doesn't work w/out a user agent so add one
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "dotnet-httpclient");
            var content = await httpClient.GetStringAsync(uri);
            return await Task.Run(() => ParseResponse(content));
        }

        /// <summary>
        /// puts together the api to call and then writes out the contents to the csv
        /// </summary>
        /// <param name="year">year for the api call</param>
        /// <param name="month">month for the api call</param>
        /// <param name="day">day for the api call</param>
        /// <param name="file">file to write results to</param>
        private static void GetForecast(int year, int month, int day, StreamWriter file)
        {
            string url = String.Format(@"http://www.nwac.us/api/v3/avalanche-forecast/?day1_date={0}-{1}-{2}&app=<appcode from nwac>", year.ToString(), month.ToString(), day.ToString());
            try
            {
                var parsedResult = GetAsync(url).Result;
                if (parsedResult == null)
                {
                    return;
                }
                var outBuffer = new List<string>();
                foreach (var f in parsedResult)
                {
                    var tmpString = f.ToString(out StringBuilder sbHeader);
                    if (isFirst)
                    {
                        var header = sbHeader.ToString();
                        file.WriteLine(header.Remove(header.Length - 1)); //remove ending comma
                        isFirst = false;
                    }
                    file.WriteLine(tmpString.Remove(tmpString.Length - 1)); //remove ending comma
                }
            }
            catch (System.AggregateException e)
            {
                //likely a bad date format error; can ignore
                return;
            }
        }
        /// <summary>
        /// Parses json response
        /// </summary>
        /// <param name="resultToParse">json repsonse from api</param>
        /// <returns>list of parsed responses one item in list per regional forecast</returns>
        private static List<AvalancheRegionForecast> ParseResponse(string resultToParse)
        {
            var parsedForecasts = new List<AvalancheRegionForecast>();
            //for each forecast
            dynamic file = JObject.Parse(resultToParse);
            if(file.objects.Count == 0)
            {
                return null;
            }
            dynamic avyRegionForecasts = file.objects[0].avalanche_region_forecast;
            foreach(dynamic forecast in avyRegionForecasts)
            {
                foreach(dynamic zone in forecast.zones)
                {
                    var parsedForecast = new AvalancheRegionForecast
                    {
                        Zone = zone.name,
                        PublishDate = forecast.publish_date,
                        ResourceUri = forecast.resource_uri,
                        SpecialStatement = forecast.special_statement,
                        Day1Date = forecast.day1_date,
                        BottomLineSummary = forecast.bottom_line_summary,
                        Day1DangerElevationHigh = forecast.day1_danger_elev_high,
                        Day1DangerElevationMiddle = forecast.day1_danger_elev_middle,
                        Day1DangerElevationLow = forecast.day1_danger_elev_low,
                        Day1DetailedForecast = forecast.day1_detailed_forecast,
                        Day1Trend = forecast.day1_trend,
                        Day1Warning = forecast.day1_warning,
                        Day1WarningText = forecast.day1_warning_text,
                        Day1WarningEnd = forecast.day1_warning_end,
                        Day2DangerElevationHigh = forecast.day2_danger_elev_high,
                        Day2DangerElevationMiddle = forecast.day2_danger_elev_middle,
                        Day2DangerElevationLow = forecast.day2_danger_elev_low,
                        Day2DetailedForecast = forecast.day2_detailed_forecast,
                        Day2Trend = forecast.day2_trend,
                        Day2Warning = forecast.day2_warning,
                        Day2WarningText = forecast.day2_warning_text,
                        Day2WarningEnd = forecast.day2_warning_end,
                        AvalancheProblems = new List<AvalancheProblem>()
                    };
                    foreach (dynamic problem in forecast.problems)
                    {
                        var parsedProblem = new AvalancheProblem
                        {
                            ProblemName = problem.problem_type.name,
                            Likelihood = problem.likelihood,
                            MaximumSize = problem.maximum_size,
                            MinimumSize = problem.minimum_size,
                            OctagonAboveTreelineEast = problem.octagon_high_east,
                            OctagonAboveTreelineNorth = problem.octagon_high_north,
                            OctagonAboveTreelineNorthEast = problem.octagon_high_northeast,
                            OctagonAboveTreelineNorthWest = problem.octagon_high_northwest,
                            OctagonAboveTreelineSouth = problem.octagon_high_south,
                            OctagonAboveTreelineSouthEast = problem.octagon_high_southeast,
                            OctagonAboveTreelineSouthWest = problem.octagon_high_southwest,
                            OctagonAboveTreelineWest = problem.octagon_high_west,

                            OctagonBelowTreelineEast = problem.octagon_low_east,
                            OctagonBelowTreelineNorth = problem.octagon_low_north,
                            OctagonBelowTreelineNorthEast = problem.octagon_low_northeast,
                            OctagonBelowTreelineNorthWest = problem.octagon_low_northwest,
                            OctagonBelowTreelineSouth = problem.octagon_low_south,
                            OctagonBelowTreelineSouthEast = problem.octagon_low_southeast,
                            OctagonBelowTreelineSouthWest = problem.octagon_low_southwest,
                            OctagonBelowTreelineWest = problem.octagon_low_west,

                            OctagonNearTreelineEast = problem.octagon_mid_east,
                            OctagonNearTreelineNorth = problem.octagon_mid_north,
                            OctagonNearTreelineNorthEast = problem.octagon_mid_northeast,
                            OctagonNearTreelineNorthWest = problem.octagon_mid_northwest,
                            OctagonNearTreelineSouth = problem.octagon_mid_south,
                            OctagonNearTreelineSouthEast = problem.octagon_mid_southeast,
                            OctagonNearTreelineSouthWest = problem.octagon_mid_southwest,
                            OctagonNearTreelineWest = problem.octagon_mid_west
                        };
                        parsedForecast.AvalancheProblems.Add(parsedProblem);
                    }
                    parsedForecasts.Add(parsedForecast);
                }
            }
            return parsedForecasts;
        }
    }
}
