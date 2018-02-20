using Microsoft.Analytics.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAvalancheProject.Pipeline.Usql.Udos
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
                foreach (var subRow in theRight) 
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
                output.Set<double>("Lat", row.Get<double>("Lat"));
                output.Set<double>("Lon", row.Get<double>("Lon"));
                output.Set<string>("StationName", closestStation);
                output.Set<float>("DistanceToStationKm", (float)distanceToStation);
                output.Set<int>("ElevationFt", closestRow.ElevationFt);
                output.Set<double>("SnotelLat", closestRow.Lat);
                output.Set<double>("SnotelLon", closestRow.Lon);
                output.Set<string>("SnotelState", closestRow.SnotelState);
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
            public string SnotelState { get; set; }
        }
    }
}