using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenAvalancheProject.Pipeline.Functions;

namespace OpenAvalancheProject.Pipeline.Tests
{
    [TestClass]
    public class SnotelTests
    {
        [TestMethod]
        public void TestCreateSnotelUrlTwoDigitHour()
        {
            var testHour = 14;
            var testDate = new DateTime(2017, 12, 20, testHour, 14, 14);
            testDate = testDate.ToUniversalTime(); //method returns local date from utc so make it utc
            var template = "%STATE%.%yyyy-MM-dd%.%HOUR%";
            var state = "WA";
            var result = DetectSnotelReadyForDownload.CreateSnotelUrl(testDate, "WA", template).Split('.');
            Assert.AreEqual(state, result[0], "State value not returned correctly.");
            Assert.AreEqual("2017-12-20", result[1], "Date value not returned correctly.");
            Assert.AreEqual(testHour.ToString(), result[2], "Date value not returned correctly.");
        }

        [TestMethod]
        public void TestCreateSnotelUrlSingleDigitHour()
        {
            var testHour = 4;
            var testDate = new DateTime(2017, 12, 20, testHour, 14, 14);
            testDate = testDate.ToUniversalTime(); //method returns local date from utc so make it utc
            var template = "%STATE%.%yyyy-MM-dd%.%HOUR%";
            var state = "WA";
            var result = DetectSnotelReadyForDownload.CreateSnotelUrl(testDate, "WA", template).Split('.');
            Assert.AreEqual(state, result[0], "State value not returned correctly.");
            Assert.AreEqual("2017-12-20", result[1], "Date value not returned correctly.");
            Assert.AreEqual(testHour.ToString(), result[2], "Date value not returned correctly.");
        }
    }
}
