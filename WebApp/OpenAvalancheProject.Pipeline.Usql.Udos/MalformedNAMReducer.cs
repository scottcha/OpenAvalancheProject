using Microsoft.Analytics.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAvalancheProject.Pipeline.Usql.Udos
{
    /// <summary>
    /// NAM Hour 0 Sometimes comes malformed, fix it by reforming it 
    /// </summary>
    [SqlUserDefinedReducer]
    public class MalformedNAMReducer : IReducer
    {
        /// <summary>
        /// Will reduce a malformed set to one with distinct filehour, lat, lon
        /// should reduce on filehour, lat and lon
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public override IEnumerable<IRow> Reduce(IRowset input, IUpdatableRow output)
        {
            var rows = (from r in input.Rows
                        select new NamRow
                        {
                            //ParsedHour = DateTime.ParseExact(r.Get<string>("DateString"), "yyyyMMdd HH:00", null).Hour,
                            DateString = r.Get<string>("DateString"),
                            Lat = r.Get<double>("Lat"),
                            Lon = r.Get<double>("Lon"),
                            APCPsurface = r.Get<double?>("APCPsurface"),
                            APCPStepSize = r.Get<int?>("APCPStepSize"),
                            CSNOWsurface = r.Get<int?>("CSNOWsurface"),
                            CRAINsurface = r.Get<int?>("CRAINsurface"),
                            TMPsurface = r.Get<double?>("TMPsurface"),
                            Tmp2mAboveGround = r.Get<double?>("Tmp2mAboveGround"),
                            RH2mAboveGround = r.Get<double?>("RH2mAboveGround"),
                            TMP80mAboveGround = r.Get<double?>("TMP80mAboveGround"),
                            TMPTrop = r.Get<double?>("TMPTrop"),
                            WindSpeed10m = r.Get<double?>("WindSpeed10m"),
                            WindDirection10m = r.Get<double?>("WindDirection10m"),
                            WindSpeed80m = r.Get<double?>("WindSpeed80m"),
                            WindDirection80m = r.Get<double?>("WindDirection80m"),
                            WindSpeedTrop = r.Get<double?>("WindSpeedTrop"),
                            WindDirectionTrop = r.Get<double?>("WindDirectionTrop"),
                            __fileHour = r.Get<int>("__fileHour"),
                            __fileDate = r.Get<DateTime>("__fileDate")
                        }).ToList();

            NamRow r1 = null;
            NamRow r2 = null;
            if (rows.Count() == 1)
            {
                try
                {
                    r1 = rows.First();
                    r2 = rows.First();
                }
                catch(InvalidOperationException e)
                {
                    throw new InvalidOperationException(string.Format("InvalidOperationException on row with count 1, __fileDate: {0}, Lat {1}, Lon {2}", rows[0].__fileDate, rows[0].Lat, rows[0].Lon));
                }
            }
            else if (rows.Count() == 2)
            {
                try
                {
                    r1 = rows.Where(rtmp => rtmp.ParsedHour == 0).First();
                    r2 = rows.Where(rtmp => rtmp.ParsedHour != 0).First();
                }
                catch (InvalidOperationException e)
                {
                    throw new InvalidOperationException(string.Format("InvalidOperationException on row with count 2, __fileDate: {0}, Lat {1}, Lon {2}; check that there aren't overlapping regions", rows[0].__fileDate, rows[0].Lat, rows[0].Lon));
                }
            }
            else
            {
                throw new ArgumentException(string.Format("Unexpected number of rows, got {0} but expected 1 or 2", rows.Count()));
            }
            //in the case we are attempting to fix there should be two row per lat/lon
            //first get the date/lat/lon and TMPSurface from row with correct hour
            output.Set<string>("DateString", r1.DateString);
            output.Set<double>("Lat", r1.Lat);
            output.Set<double>("Lon", r1.Lon);
            output.Set<double?>("TMPsurface", r1.TMPsurface);
            output.Set<double?>("APCPsurface", r2.APCPsurface);
            output.Set<int?>("APCPStepSize", r2.APCPStepSize);
            output.Set<int?>("CSNOWsurface", r2.CSNOWsurface);
            output.Set<int?>("CRAINsurface", r2.CRAINsurface);
            output.Set<double?>("Tmp2mAboveGround", r2.Tmp2mAboveGround);
            output.Set<double?>("RH2mAboveGround", r2.RH2mAboveGround);
            output.Set<double?>("TMP80mAboveGround", r2.TMP80mAboveGround);
            output.Set<double?>("TMPTrop", r2.TMPTrop);
            output.Set<double?>("WindSpeed10m", r2.WindSpeed10m);
            output.Set<double?>("WindDirection10m", r2.WindDirection10m);
            output.Set<double?>("WindSpeed80m", r2.WindSpeed80m);
            output.Set<double?>("WindDirection80m", r2.WindDirection80m);
            output.Set<double?>("WindSpeedTrop", r2.WindSpeedTrop);
            output.Set<double?>("WindDirectionTrop", r2.WindDirectionTrop);
            output.Set<int>("__fileHour", r2.__fileHour);
            output.Set<DateTime>("__fileDate", r2.__fileDate);
            yield return output.AsReadOnly();
        }
    }

    internal class NamRow
    {
        public int ParsedHour
        {
            get
            {
                return DateTime.ParseExact(DateString, "yyyyMMdd HH:00", null).Hour;
            }
        }
        public string DateString { get; set; }
        public double    Lat { get; set; }
        public double Lon { get; set; }
        public double?    APCPsurface { get; set; }
        public int?    APCPStepSize { get; set; }
        public int?    CSNOWsurface { get; set; }
        public int?    CRAINsurface { get; set; }
        public double?    TMPsurface { get; set; }
        public double?    Tmp2mAboveGround { get; set; }
        public double?    RH2mAboveGround { get; set; }
        public double?    TMP80mAboveGround { get; set; }
        public double?    TMPTrop { get; set; }
        public double?    WindSpeed10m { get; set; }
        public double?    WindDirection10m { get; set; }
        public double?    WindSpeed80m { get; set; }
        public double?    WindDirection80m { get; set; }
        public double?    WindSpeedTrop { get; set; }
        public double?    WindDirectionTrop { get; set; }
        public int    __fileHour { get; set; }
        public DateTime __fileDate { get; set; }
    }
}
