using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Text;
using OpenAvalancheProject.Pipeline.Utilities;

namespace OpenAvalancheProject.Pipeline
{
    public class NamTableRow : TableEntity
    {
        public NamTableRow(DateTime forecastDate, DateTime referenceTime, Tuple<double, double> coordinates)
        {
            Date = forecastDate;
            Lat = coordinates.Item1;
            Lon = coordinates.Item2;
            var hourPart = forecastDate.Subtract(referenceTime).TotalHours;
            PartitionKey = referenceTime.ToString("yyyyMMddT") + hourPart.ToString("00") + "forecastHour00";
            RowKey = Math.Round(coordinates.Item1, 6).ToString() + ":" + Math.Round(coordinates.Item2, 6).ToString();
        }

        public NamTableRow() { }

        public DateTime Date{ get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double? APCPSurface { get; set; }
        public int? APCPStepSize { get; set; }
        public int? CSNOWSurface { get; set; }
        public int? CRAINSurface { get; set; }
        public double? TMPSurface { get; set; }
        public double? TMP2mAboveGround { get; set; }
        public double? RH2mAboveGround { get; set; }
        public double? TMP80mAboveGround { get; set; }
        public double? TMPTrop { get; set; }
        public double? UGRD10m { get; set; }
        public double? VGRD10m { get; set; }
        public double? UGRD80m { get; set; }
        public double? VGRD80m { get; set; }
        public double? UGRDTrop { get; set; }
        public double? VGRDTrop { get; set; }
        public double? WindSpeed10m {
            get
            {
                if (UGRD10m == null || VGRD10m == null)
                    return null;
                return GribUtilities.DecodeWindVectors(UGRD10m.Value, VGRD10m.Value).Speed;
            }
        }
        public double? WindDirection10m {
            get
            {
                if (UGRD10m == null || VGRD10m == null)
                    return null;
                return GribUtilities.DecodeWindVectors(UGRD10m.Value, VGRD10m.Value).Direction;
            }
        }
        public double? WindSpeed80m {
            get
            {
                if (UGRD80m == null || UGRD80m == null)
                    return null;
                return GribUtilities.DecodeWindVectors(UGRD80m.Value, VGRD80m.Value).Speed;
            }
        }
        public double? WindDirection80m {
            get
            {
                if (UGRD80m == null || UGRD80m == null)
                    return null;
                return GribUtilities.DecodeWindVectors(UGRD80m.Value, VGRD80m.Value).Direction;
            }
        }
        public double? WindSpeedTrop {
            get
            {
                if (UGRDTrop == null || UGRDTrop == null)
                    return null;
                return GribUtilities.DecodeWindVectors(UGRDTrop.Value, VGRDTrop.Value).Direction;
            }
        }
        public double? WindDirectionTrop {
            get
            {
                if (UGRDTrop == null || UGRDTrop == null)
                    return null;
                return GribUtilities.DecodeWindVectors(UGRDTrop.Value, VGRDTrop.Value).Speed;
            }
        }
        public static string Columns
        {
            get
            {
                return ("Date, Lat, Lon, APCPSurface, APCPStepSize, CSNOWSurface, CRAINSurface, TMPSurface, TMP2mAboveGround, RH2mAboveGround, TMP80mAboveGround, TMPTrop, WindSpeed10m, WindDirection10m, WindSpeed80m, WindDirection80m, WindSpeedTrop, WindDirectionTrop");
            }
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb = sb.Append(Date.ToString("yyyyMMdd HH:00") + ", ");
            sb = sb.Append(Lat + ", " + Lon + ", ");
            sb = sb.Append(APCPSurface + ", " + APCPStepSize + ", " + CSNOWSurface + ", " + CRAINSurface + ", ");
            sb = sb.Append(TMPSurface + ", " + TMP2mAboveGround + ", " + RH2mAboveGround + ", " + TMP80mAboveGround + ", " + TMPTrop + ", ");
            sb = sb.Append(WindSpeed10m + ", " + WindDirection10m + ", ");
            sb = sb.Append(WindSpeed80m + ", " + WindDirection80m + ", ");
            sb = sb.Append(WindSpeedTrop + ", " + WindDirectionTrop);
            return sb.ToString();
        }

    }
}
