using Xunit;
using System.IO;
using GetCAData;

namespace GetCAData.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        using (StreamReader file = new StreamReader(@"../../../../SampleCAAML.xml"))
        {
            var xmlContents = file.ReadToEnd();
            var parser = new ParseCAAML(xmlContents);
        }
    }
}