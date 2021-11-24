using System;
using System.Net.Http;

namespace GetCAData
{
    class Program
    {
        static void Main(string[] args)
        {
            var d = new DateTime(2015, 1, 1);
            var r = 1;
            GetForecast(d, r);
        }

        private static async Task<string> GetAsync(string url)
        {
            var httpClient = new HttpClient();
            var content = await httpClient.GetStringAsync(url);
            return content; 
        }

        private static void GetForecast(DateTime date, int region)
        {
            string url = String.Format(@"https://avalanche.pc.gc.ca/CAAML-eng.aspx?d={0:yyyy}-{0:dd}-{0:MM}&r={1}", date, region);
            try
            {
                var result = GetAsync(url).Result;
            }
            catch(Exception e)
            {
                Console.Out.WriteLine(e);
            }
        }
    }
}