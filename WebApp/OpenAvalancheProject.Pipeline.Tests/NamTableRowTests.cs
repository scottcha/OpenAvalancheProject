using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenAvalancheProject.Pipeline.Tests
{
    [TestClass]
    public class NamTableRowTests
    {
        [TestMethod]
        public void TestToString()
        {
            DateTime date = DateTime.UtcNow;
            var dateString = date.ToString("yyyyMMdd HH:00");
            NamTableRow row = new NamTableRow(date, date, new Tuple<double, double>(1.0, 2.0));
            row.APCPStepSize = 1;
            row.APCPsurface = 1.0;
            row.CRAINsurface = 1;
            row.CSNOWsurface = 1;
            row.RH2mAboveGround = 1.0;
            row.TMP2mAboveGround = 1.0;
            row.TMP80mAboveGround = 1.0;
            row.TMPsurface = 1.0;
            row.TMPTrop = 1.0;
            row.UGRD10m = 1.0;
            row.UGRD80m = 1.0;
            row.UGRDTrop = 1.0;
            row.VGRD10m = 1.0;
            row.VGRD80m = 1.0;
            row.VGRDTrop = 1.0;
            var s = row.ToString();
            var substring = s.Split(',');
            Assert.AreEqual(substring.Length, 18);
            Assert.AreEqual(substring[0], dateString, "Date string incorrect format");
        }
    }
}
