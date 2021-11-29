using Xunit;
using System.IO;
using GetData;
using System.Collections.Generic;

namespace GetData.Tests;

public class UnitTest1
{
    [Fact]
    public void TestCAAMLParse()
    {
        using (StreamReader file = new StreamReader(@"../../../../CAParksSampleCAAML.xml"))
        {
            var parser = new CAAMLParser();
            var forecast = parser.Parse(file);
        }
    }
    
    [Fact]
    public void TestWriteCAParksForecast()
    {
        var forecasts = new List<AvalancheRegionForecast>();
        for(int i = 0; i < 3; i++)
        {
            using (StreamReader file = new StreamReader(@"../../../../CAParksSampleCAAML.xml"))
            {
                    var parser = new CAAMLParser();
                    forecasts.Add(parser.Parse(file));
            }
        }
        Program.WriteForecastsToFile("TestForecastOut.csv", forecasts);
    }
   
    /// <summary>
    /// </summary>
    /// TODO: there is probably a way to combine this with the TestCAAMLParse using attributes
    [Fact]
    public void TestCAJsonParse()
    {
        using (StreamReader file = new StreamReader(@"../../../../CASample.json"))
        {
            var parser = new CAJsonParser();
            var forecast = parser.Parse(file);
        }
    }
}