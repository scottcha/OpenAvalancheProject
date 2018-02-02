using Microsoft.Analytics.Interfaces;
using Microsoft.Analytics.Types.Sql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenAvalancheProject.Pipeline.Usql
{
    [SqlUserDefinedCombiner(Mode = CombinerMode.Left)]
    public class CombinerNearestStation : ICombiner
    {
        private const float DistanceThresholdKm = 160.0F;
        /// <summary>
        /// Combine is called once per match on the join clause; its not prefiltered to left or right but gives the full data sets for each maching the join clause
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public override IEnumerable<IRow> Combine(IRowset left, IRowset right, IUpdatableRow output)
        {
            var theRight = (from row in right.Rows
                            select new SnotelRow
                            {
                                StationName = row.Get<string>("StationName"),
                                Lat = row.Get<double>("Lat"),
                                Lon = row.Get<double>("Lon"),
                                ElevationFt = row.Get<int>("ElevationFt"),
                                //SnowWaterEquivalentIn = row.Get<float?>("SnowWaterEquivalentIn"),
                                //PrecipitationAccumulation = row.Get<float?>("PrecipitationAccumulation"),
                                //SnowDepthIn = row.Get<int?>("SnowDepthIn"),
                                //AirTemperatureObservedF = row.Get<int?>("AirTemperatureObservedF"),
                                SnotelState = row.Get<string>("SnotelState")
                            }).ToList();

            foreach (var row in left.Rows)
            {
                var Lat = row.Get<double>("Lat");
                var Lon = row.Get<double>("Lon");

                string closestStation = "None";
                double distanceToStation = DistanceThresholdKm + 1; //default is just longer than distance threshold 
                SnotelRow closestRow = null;

                //narrow the search range down to just ones within 1 degree lat/lon of the current value
                //TODO; this should be increased or removed for longitude if we include alaska or others more north
                foreach (var subRow in theRight) //.Where(a => (Lat > a.GridLat-2 && Lat < a.GridLat+2 && Lon > a.GridLon-2 && Lon < a.GridLon + 2)))
                {
                    //Calculate distance
                    var tmpDistance = DistanceBetweenCoordinates(Lat, Lon, subRow.Lat, subRow.Lon);
                    //Store value if its bigger than the previous value
                    if (tmpDistance < DistanceThresholdKm && tmpDistance < distanceToStation)
                    {
                        closestStation = subRow.StationName;
                        distanceToStation = tmpDistance;
                        closestRow = subRow;
                    }
                }

                if (closestRow == null)
                {
                    distanceToStation = 0;
                    closestRow = new SnotelRow()
                    {
                        StationName = "None",
                        Lat = 0,
                        Lon = 0,
                        ElevationFt = 0,
                        //SnowWaterEquivalentIn = null,
                        //PrecipitationAccumulation = null,
                        //SnowDepthIn = null,
                        //AirTemperatureObservedF = null,
                        SnotelState = "None"
                    };
                }
                output.Set<DateTime>("Date", row.Get<DateTime>("__fileDate"));
                //output.Set<DateTime>("Date", row.Get<DateTime>("Date"));
                //output.Set<string>("DateString", row.Get<string>("DateString"));
                output.Set<double>("Lat", row.Get<double>("Lat"));
                output.Set<double>("Lon", row.Get<double>("Lon"));
                //output.Set<double?>("APCPsurface", row.Get<double?>("APCPsurface"));
                //output.Set<int?>("APCPStepSize", row.Get<int?>("APCPStepSize"));
                //output.Set<int>("CSNOWsurface", row.Get<int>("CSNOWsurface"));
                //output.Set<int>("CRAINsurface", row.Get<int>("CRAINsurface"));
                //output.Set<double>("TMPsurface", row.Get<double>("TMPsurface"));
                //output.Set<double>("Tmp2mAboveGround", row.Get<double>("Tmp2mAboveGround"));
                //output.Set<double>("RH2mAboveGround", row.Get<double>("RH2mAboveGround"));
                //output.Set<double>("TMP80mAboveGround", row.Get<double>("TMP80mAboveGround"));
                //output.Set<double>("TMPTrop", row.Get<double>("TMPTrop"));
                //output.Set<double>("WindSpeed10m", row.Get<double>("WindSpeed10m"));
                //output.Set<double>("WindDirection10m", row.Get<double>("WindDirection10m"));
                //output.Set<double>("WindSpeed80m", row.Get<double>("WindSpeed80m"));
                //output.Set<double>("WindDirection80m", row.Get<double>("WindDirection80m"));
                //output.Set<double>("WindSpeedTrop", row.Get<double>("WindSpeedTrop"));
                //output.Set<double>("WindDirectionTrop", row.Get<double>("WindDirectionTrop"));
                output.Set<string>("StationName", closestStation);
                output.Set<float>("DistanceToStationKm", (float)distanceToStation);
                output.Set<int>("ElevationFt", closestRow.ElevationFt);
                output.Set<double>("SnotelLat", closestRow.Lat);
                output.Set<double>("SnotelLon", closestRow.Lon);
                //output.Set<float?>("SnowWaterEquivalentIn", closestRow.SnowWaterEquivalentIn);
                //output.Set<float?>("PrecipitationAccumulation", closestRow.PrecipitationAccumulation);
                //output.Set<int?>("SnowDepthIn", closestRow.SnowDepthIn);
                //output.Set<int?>("AirTemperatureObservedF", closestRow.AirTemperatureObservedF);
                output.Set<string>("SnotelState", closestRow.SnotelState);
                //output.Set<int>("__fileHour", row.Get<int>("__fileHour"));
                //output.Set<DateTime>("__fileDate", row.Get<DateTime>("__fileDate"));
                yield return output.AsReadOnly();
            }
        }

        public static double DistanceBetweenCoordinates(double fromLat, double fromLon, double toLat, double toLon)
        {
            var baseRad = Math.PI * fromLat / 180.0;
            var targetRad = Math.PI * toLat / 180.0;
            var theta = fromLon - toLon;
            var thetaRad = Math.PI * theta / 180.0;

            double dist =
                Math.Sin(baseRad) * Math.Sin(targetRad) + Math.Cos(baseRad) *
                Math.Cos(targetRad) * Math.Cos(thetaRad);
            dist = Math.Acos(dist);

            dist = dist * 180.0 / Math.PI;
            dist = dist * 60.0 * 1.1515;
            //miles to kilometers
            dist = dist * 1.609344;
            return dist;

        }

        private class SnotelRow
        {
            public string StationName { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
            public int ElevationFt { get; set; }
            //public float? SnowWaterEquivalentIn { get; set; }
            //public float? PrecipitationAccumulation { get; set; }
            //public int? SnowDepthIn { get; set; }
            //public int? AirTemperatureObservedF { get; set; }
            public string SnotelState { get; set; }
        }
    }
}