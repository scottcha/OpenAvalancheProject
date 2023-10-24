using System;
using System.Net.Http;
using System.Text;

namespace GetData
{
    /// <summary>
    /// Currently only supported running by changing the center in code
    /// </summary>
    public static class Program
    {
        //Define the years and months to pull
        private static List<int> years = new List<int>() { 2015, 2016, 2017, 2018, 2019, 2020, 2021 };
        private static Dictionary<int, List<int>> months = new Dictionary<int, List<int>>()
        {
            [2015] = new List<int> { 11, 12 },
            [2016] = new List<int> { 1, 2, 3, 4, 11, 12 },
            [2017] = new List<int> { 1, 2, 3, 4, 11, 12 },
            [2018] = new List<int> { 1, 2, 3, 4, 11, 12 },
            [2019] = new List<int> { 1, 2, 3, 4, 11, 12 },
            [2020] = new List<int> { 1, 2, 3, 4, 11, 12 },
            [2021] = new List<int> { 1, 2, 3, 4 }
        };
           
        //for debug
        //private static List<int> years = new List<int>() { 2015, 2016 };
        //private static Dictionary<int, List<int>> months = new Dictionary<int, List<int>>()
        //{
        //    [2015] = new List<int> { 11, 12 },
        //    [2016] = new List<int> { 1, 2, 3, 4}
        //};

        /// <summary>
        /// Call either GetForecastCA for CAC forecasts
        /// or GetForecastCAParks for CA Parks forecasts
        /// TODO: finish the program and allow it to also call NWAC, most of the code is here from a previous version
        ///       but not integrated in to the current architecture
        /// 
        /// </summary>
        /// <param name="args"></param>        
        static void Main(string[] args)
        {
            GetForecastCA();
        }

        private static async Task<AvalancheRegionForecast> GetAsyncAndParse(string url, IParser parser)
        {
            var httpClient = new HttpClient();
            var content = await httpClient.GetStringAsync(url);
            using(StringReader sr = new StringReader(content))
            {
                return await Task.Run(() => parser.Parse(sr));
            }
        }

        /// <summary>
        /// Get a CA Parks forecast, encoded as CAAML
        /// </summary>        
        private static void GetForecastCAParks()
        {
            var regions = new List<int>() { 1, 2, 3, 4, 5};
            var forecasts = new List<AvalancheRegionForecast>();
            foreach (var region in regions)
            {
                foreach (var year in years)
                {
                    foreach (var month in months[year])
                    {
                        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
                        {
                            var date = new DateTime(year, month, day);
                            Console.WriteLine(string.Format("On region {0} and date {1:yyyyMMdd}", region, date));
                            string url = String.Format(@"https://avalanche.pc.gc.ca/CAAML-eng.aspx?d={0:yyyy}-{0:MM}-{0:dd}&r={1}", date, region);

                            try
                            {
                                var parser = new CAAMLParser();
                                var result = GetAsyncAndParse(url, parser).Result;
                                forecasts.Add(result);
                            }
                            catch (Exception e)
                            {
                                Console.Out.WriteLine(e);
                            }
                            //pause for 1/2 second to keep load lite on server
                            System.Threading.Thread.Sleep(500);
                        }
                    }
                }
            }
            Program.WriteForecastsToFile(@"..\..\..\cacParksForecasts.csv", forecasts);
        }
           
        /// <summary>
        /// Gets the non-parks CA forecats, encoded as json
        /// </summary>
        public static void GetForecastCA()
        {
            var regions = new List<string>() { "northwest-coastal", "northwest-inland", "sea-to-sky", "south-coast-inland", "south-coast", "north-rockies", "cariboos", "north-columbia", "south-columbia", "purcells", "kootenay-boundary", "south-rockies", "lizard-range", "vancouver-island", "kananaskis", "chic-chocs", "yukon" };
            var forecasts = new List<AvalancheRegionForecast>();
            foreach (var region in regions)
            {
                foreach (var year in years)
                {
                    foreach (var month in months[year])
                    {
                        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
                        {
                            var date = new DateTime(year, month, day);
                            Console.WriteLine(string.Format("On region {0} and date {1:yyyyMMdd}", region, date));
                            var url = String.Format(@"https://avalanche.ca/api/bulletin-archive/{0:yyyy-MM-dd}/{1}.json", date, region);

                            try
                            {
                                var parser = new CAJsonParser();
                                var result = GetAsyncAndParse(url, parser).Result;
                                result.ResourceUri = url;
                                forecasts.Add(result);
                            }
                            catch (Exception e)
                            {
                                Console.Out.WriteLine(e);
                            }
                            //pause for 1/2 second to keep load lite on server
                            System.Threading.Thread.Sleep(500);
                        }
                    }
                }
            }
            Program.WriteForecastsToFile(@"..\..\..\cacForecasts.csv", forecasts);
        }

        public static void WriteForecastsToFile(string filePath, List<AvalancheRegionForecast> forecasts)
        {
            bool isFirst = true;
            using (StreamWriter file = new StreamWriter(filePath))
            {
                foreach (var f in forecasts)
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
        }
    }
}