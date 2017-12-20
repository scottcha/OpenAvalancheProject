using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grib.Api;
using OpenAvalancheProject.Pipeline.Utilities;

namespace OpenAvalancheProject.Pipeline.Tests
{
    [TestClass]
    public class GribUtilitiesTests
    {
        [TestMethod]
        public void TestExpectedFields()
        {
            //if an exception is thrown one cause it there were messaged in the file we didn't expect
            using (GribFile file = new GribFile(@"..\..\TestFiles\expected_fields.grib2"))
            {
                var rowList = GribUtilities.ParseNamGribFile(file);
                Assert.AreEqual(31313, rowList.Count, "Expected count to be equal to 31313");
                var firstRow = rowList[0];
                Assert.AreEqual(Math.Round(firstRow.RH2mAboveGround.Value, 2), 98.37, "Expected first element returned to have RH at 2m of this value.");
                Assert.AreEqual(Math.Round(firstRow.UGRD10m.Value, 2), -3.7, "Expected first element returned to have UGRD at 10m of this value.");
            }
        }

        [TestMethod]
        public void TestUnexpectedFields()
        {
            //if an exception is thrown one cause it there were messaged in the file we didn't expect
            using (GribFile file = new GribFile(@"..\..\TestFiles\unexpected_fields.grib2"))
            {
                try
                {
                    var rowList = GribUtilities.ParseNamGribFile(file);
                }
                catch(Exception e)
                {
                    Assert.IsInstanceOfType(e, typeof(NotSupportedException), "Didn't get the exception type we expected");
                    return;
                }
                Assert.Fail("Expected to get an exception and we didn't");
            }
        }

        [TestMethod]
        public void TestExpectedFieldsSecondHour()
        {
            //if an exception is thrown one cause it there were messaged in the file we didn't expect
            using (GribFile file = new GribFile(@"..\..\TestFiles\expected_fields_hour2.grib2"))
            {
                var rowList = GribUtilities.ParseNamGribFile(file);
                Assert.AreEqual(31313, rowList.Count, "Expected count to be equal to 31313");
                var firstRow = rowList[0];
                Assert.AreEqual(Math.Round(firstRow.RH2mAboveGround.Value, 2), 76.8, "Expected first element returned to have RH at 2m of this value.");
                Assert.AreEqual(Math.Round(firstRow.UGRD10m.Value, 2), -1.15, "Expected first element returned to have UGRD at 10m of this value.");
            }
        }

        [TestMethod]
        public void TestUnexpectedFields2()
        {
            //if an exception is thrown one cause it there were messaged in the file we didn't expect
            using (GribFile file = new GribFile(@"..\..\TestFiles\unexpected_fields2.grib2"))
            {
                var rowList = GribUtilities.ParseNamGribFile(file);
                Assert.AreEqual(31313, rowList.Count, "Expected count to be equal to 31313");
            }
        }

        [TestMethod]
        public void TestForecastTimeGreaterThan24()
        {
            //if an exception is thrown one cause it there were messaged in the file we didn't expect
            using (GribFile file = new GribFile(@"..\..\TestFiles\dateover12.grib2"))
            {
                var rowList = GribUtilities.ParseNamGribFile(file);
                Assert.AreEqual("20171130T84forecastHour00", rowList[0].PartitionKey, "Incorrect date processing accounting for incorrect hours");
                Assert.AreEqual(31313, rowList.Count, "Expected count to be equal to 31313");
            }
        }

        [TestMethod]
        public void TestForOffsetStepStart()
        {
            //if an exception is thrown one cause it there were messaged in the file we didn't expect
            using (GribFile file = new GribFile(@"..\..\TestFiles\checkfornullapcp.grib2"))
            {
                var rowList = GribUtilities.ParseNamGribFile(file);
                Assert.AreEqual(31313, rowList.Count, "Expected count to be equal to 31313");
                var secondRow = rowList[1];
                Assert.AreEqual(secondRow.APCPStepSize.Value, 11, "Incorrect step size.");
                Assert.AreEqual(Math.Round(secondRow.APCPSurface.Value, 4), .0625, "Incorrect APCPSurface value");
                Assert.AreEqual(secondRow.Date, new DateTime(2017, 12, 4, 23, 0, 0), "Incorrect date");
            }
        }
    }
}
