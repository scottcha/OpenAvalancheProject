using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAvalancheProject.Pipeline
{
    public class SnodasRow
    {
        public SnodasRow(double Lat, double Lon)
        {
            this.Lat = Lat;
            this.Lon = Lon;
        }
        public DateTime Date { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double SNOWDAS_SnowDepth_mm { get; set; }
        public double SNOWDAS_SWE_mm { get; set; }
        public double SNOWDAS_SnowmeltRunoff_micromm { get; set; }
        public double SNOWDAS_Sublimation_micromm { get; set; }
        public double SNOWDAS_SublimationBlowing_micromm { get; set; }
        public double SNOWDAS_SolidPrecip_kgpersquarem { get; set; }
        public double SNOWDAS_LiquidPrecip_kgpersquarem { get; set; }
        public double SNOWDAS_SnowpackAveTemp_k { get; set; }
        public static string GetHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("Date, Lat, Lon,");
                sb.Append("SNOWDAS_SnowDepth_mm" + ",");
                sb.Append("SNOWDAS_SWE_mm" + ",");
                sb.Append("SNOWDAS_SnowmeltRunoff_micromm" + ",");
                sb.Append("SNOWDAS_Sublimation_micromm" + ",");
                sb.Append("SNOWDAS_SublimationBlowing_micromm" + ",");
                sb.Append("SNOWDAS_SolidPrecip_kgpersquarem" + ",");
                sb.Append("SNOWDAS_LiquidPrecip_kgpersquarem" + ",");
                sb.Append("SNOWDAS_SnowpackAveTemp_k");
                return sb.ToString(); 
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Date.ToString("yyyyMMdd HH:00") + ",");
            sb.Append(Lat.ToString() + ",");
            sb.Append(Lon.ToString() + ",");
            sb.Append(SNOWDAS_SnowDepth_mm.ToString() + ",");
            sb.Append(SNOWDAS_SWE_mm.ToString() + ",");
            sb.Append(SNOWDAS_SnowmeltRunoff_micromm.ToString() + ",");
            sb.Append(SNOWDAS_Sublimation_micromm.ToString() + ",");
            sb.Append(SNOWDAS_SublimationBlowing_micromm.ToString() + ",");
            sb.Append(SNOWDAS_SolidPrecip_kgpersquarem.ToString() + ",");
            sb.Append(SNOWDAS_LiquidPrecip_kgpersquarem.ToString() + ",");
            sb.Append(SNOWDAS_SnowpackAveTemp_k.ToString());
            return sb.ToString();
        }
    }
}
