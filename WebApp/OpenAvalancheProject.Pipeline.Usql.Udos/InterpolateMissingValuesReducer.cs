using Microsoft.Analytics.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;

namespace OpenAvalancheProject.Pipeline.Usql.Udos
{
    [SqlUserDefinedReducer]
    public class InterpolateMissingValuesReducer : IReducer
    {
        public override IEnumerable<IRow> Reduce(IRowset input, IUpdatableRow output)
        {
            var rows = (from r in input.Rows
                        select new SnotelRow
                        {
                            DatePart = r.Get<DateTime>("DatePart"),
                            Date = r.Get<DateTime>("Date"),
                            DateString = r.Get<String>("DateString"),
                            StationName = r.Get<String>("StationName"),
                            ElevationFt = r.Get<int>("ElevationFt"),
                            Lat = r.Get<double>("Lat"),
                            Lon = r.Get<double>("Lon"),
                            SnowWaterEquivalentIn = r.Get<float?>("SnowWaterEquivalentIn"),
                            PrecipitationAccumulation = r.Get<float?>("PrecipitationAccumulation"),
                            SnowDepthIn = r.Get<int?>("SnowDepthIn"),
                            AirTemperatureObservedF = r.Get<int?>("AirTemperatureObservedF"),
                            SnotelState = r.Get<String>("SnotelState"),
                            __fileHour = r.Get<int>("__fileHour"),
                            __fileDate = r.Get<DateTime>("__fileDate")
                        }).ToList();

            List<double> pointsSwe = new List<double>();
            List<double> pointsPrecip = new List<double>();
            List<double> pointsSnowDepth = new List<double>();
            List<double> pointsAirTemp = new List<double>();
            List<double> valuesSwe = new List<double>();
            List<double> valuesPrecip = new List<double>();
            List<double> valuesSnowDepth = new List<double>();
            List<double> valuesAirTemp = new List<double>();
            bool interpolateSwe = false;
            bool interpolatePrecip = false;
            bool interpolateSnowDepth = false;
            bool interpolateAirTemp = false;

            for (int count = 0; count < rows.Count(); count++)
            {
                var row = rows.ElementAt(count);
                if (row.SnowWaterEquivalentIn != null)
                {
                    pointsSwe.Add(count);
                    valuesSwe.Add(row.SnowWaterEquivalentIn.Value);
                }
                else
                {
                    interpolateSwe = true;
                }

                if (row.PrecipitationAccumulation != null)
                {
                    pointsPrecip.Add(count);
                    valuesPrecip.Add(row.PrecipitationAccumulation.Value);
                }
                else
                {
                    interpolatePrecip = true;
                }

                if (row.SnowDepthIn != null)
                {
                    pointsSnowDepth.Add(count);
                    valuesSnowDepth.Add(row.SnowDepthIn.Value);
                }
                else
                {
                    interpolateSnowDepth = true;
                }

                if (row.AirTemperatureObservedF != null)
                {
                    pointsAirTemp.Add(count);
                    valuesAirTemp.Add(row.AirTemperatureObservedF.Value);
                }
                else
                {
                    interpolateAirTemp = true;
                }
            }

            var methodSwe = (pointsSwe.Count > 1 && interpolateSwe ? Interpolate.Linear(pointsSwe, valuesSwe) : null);
            var methodPrecip = (pointsPrecip.Count > 1 && interpolatePrecip ? Interpolate.Linear(pointsPrecip, valuesPrecip) : null);
            var methodSnowDepth = (pointsSnowDepth.Count > 1 && interpolateSnowDepth ? Interpolate.Linear(pointsSnowDepth, valuesSnowDepth) : null);
            var methodAirTemp = (pointsAirTemp.Count > 1 && interpolateAirTemp ? Interpolate.Linear(pointsAirTemp, valuesAirTemp) : null);

            for (int count = 0; count < rows.Count(); count++)
            {
                var row = rows.ElementAt(count);
                if (row.SnowWaterEquivalentIn != null)
                {
                    output.Set<float?>("SnowWaterEquivalentIn", row.SnowWaterEquivalentIn.Value);
                }
                else if (row.SnowWaterEquivalentIn == null && methodSwe != null)
                {
                    float swe = (float)methodSwe.Interpolate(count);
                    output.Set<float?>("SnowWaterEquivalentIn", swe);
                }
                else
                {
                    output.Set<float?>("SnowWaterEquivalentIn", null);
                }


                if (row.PrecipitationAccumulation != null)
                {
                    output.Set<float?>("PrecipitationAccumulation", row.PrecipitationAccumulation.Value);
                }
                else if (row.PrecipitationAccumulation == null && methodPrecip != null)
                {
                    float precip = (float)methodPrecip.Interpolate(count);
                    output.Set<float?>("PrecipitationAccumulation", precip);
                }
                else
                {
                    output.Set<float?>("PrecipitationAccumulation", null);
                }

                if (row.SnowDepthIn != null)
                {
                    output.Set<int?>("SnowDepthIn", row.SnowDepthIn.Value);
                }
                else if (row.SnowDepthIn == null && methodSnowDepth != null)
                {
                    int depth = (int)methodSnowDepth.Interpolate(count);
                    output.Set<int?>("SnowDepthIn", depth);
                }
                else
                {
                    output.Set<int?>("SnowDepthIn", null);
                }

                if (row.AirTemperatureObservedF != null)
                {
                    output.Set<int?>("AirTemperatureObservedF", row.AirTemperatureObservedF.Value);
                }
                else if (row.AirTemperatureObservedF == null && methodAirTemp != null)
                {
                    int temp = (int)methodAirTemp.Interpolate(count);
                    output.Set<int?>("AirTemperatureObservedF", temp);
                }
                else
                {
                    output.Set<int?>("AirTemperatureObservedF", null);
                }

                output.Set<DateTime>("DatePart", row.DatePart);
                output.Set<DateTime>("Date", row.Date);
                output.Set<String>("DateString", row.DateString);
                output.Set<String>("StationName", row.StationName);
                output.Set<int>("ElevationFt", row.ElevationFt);
                output.Set<double>("Lat", row.Lat);
                output.Set<double>("Lon", row.Lon);
                output.Set<String>("SnotelState", row.SnotelState);
                output.Set<int>("__fileHour", row.__fileHour);
                output.Set<DateTime>("__fileDate", row.__fileDate);
                yield return output.AsReadOnly();
            }
        }
    }

    internal class SnotelRow
    {
        public DateTime DatePart { get; set; }
        public DateTime Date { get; set; }
        public string DateString { get; set; }
        public string StationName { get; set; }
        public int ElevationFt { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public float? SnowWaterEquivalentIn { get; set; }
        public float? PrecipitationAccumulation { get; set; }
        public int? SnowDepthIn { get; set; }
        public int? AirTemperatureObservedF { get; set; }
        public string SnotelState { get; set; }
        public int __fileHour { get; set; }
        public DateTime __fileDate { get; set; }
    }
}
