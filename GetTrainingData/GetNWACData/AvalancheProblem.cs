using System;
using System.Text;
using System.Text.RegularExpressions;

namespace GetNWACData
{
    public class AvalancheProblem
    {
        public string ProblemName{get;set;}
        public string Likelihood{get;set;}
        public string MaximumSize{get;set;}
        public string MinimumSize{get;set;}
        public bool OctagonAboveTreelineEast {get;set;}
        public bool  OctagonAboveTreelineNorth {get;set;}
        public bool OctagonAboveTreelineNorthEast {get;set;}
        public bool OctagonAboveTreelineNorthWest {get;set;}
        public bool OctagonAboveTreelineSouth {get;set;}
        public bool OctagonAboveTreelineSouthEast {get;set;}
        public bool OctagonAboveTreelineSouthWest {get;set;}
        public bool OctagonAboveTreelineWest {get;set;}
        public bool OctagonBelowTreelineEast {get;set;}
        public bool OctagonBelowTreelineNorth {get;set;}
        public bool OctagonBelowTreelineNorthEast {get;set;}
        public bool OctagonBelowTreelineNorthWest {get;set;}
        public bool OctagonBelowTreelineSouth {get;set;}
        public bool OctagonBelowTreelineSouthEast {get;set;}
        public bool OctagonBelowTreelineSouthWest {get;set;}
        public bool OctagonBelowTreelineWest {get;set;}
        public bool OctagonNearTreelineEast {get;set;}
        public bool OctagonNearTreelineNorth {get;set;}
        public bool OctagonNearTreelineNorthEast {get;set;}
        public bool OctagonNearTreelineNorthWest {get;set;}
        public bool OctagonNearTreelineSouth {get;set;}
        public bool OctagonNearTreelineSouthEast {get;set;}
        public bool OctagonNearTreelineSouthWest {get;set;}
        public bool OctagonNearTreelineWest {get;set;}
        public string Header()
        {
            var sb = new StringBuilder();
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_Likelihood,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_MaximumSize,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_MinimumSize,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonAboveTreelineEast,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonAboveTreelineNorth,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonAboveTreelineNorthEast,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonAboveTreelineNorthWest,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonAboveTreelineSouth,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonAboveTreelineSouthEast,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonAboveTreelineSouthWest,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonAboveTreelineWest,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonNearTreelineEast,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonNearTreelineNorth,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonNearTreelineNorthEast,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonNearTreelineNorthWest,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonNearTreelineSouth,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonNearTreelineSouthEast,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonNearTreelineSouthWest,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonNearTreelineWest,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonBelowTreelineEast,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonBelowTreelineNorth,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonBelowTreelineNorthEast,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonBelowTreelineNorthWest,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonBelowTreelineSouth,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonBelowTreelineSouthEast,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonBelowTreelineSouthWest,");
            sb.Append(Regex.Replace(ProblemName, @"\s+", "") + "_OctagonBelowTreelineWest,");
            return sb.ToString(); 
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            
            sb.Append(Likelihood + ",");
            sb.Append(MaximumSize + ",");
            sb.Append(MinimumSize+ ",");
            sb.Append(Convert.ToInt32(OctagonAboveTreelineEast)+ ",");
            sb.Append(Convert.ToInt32(OctagonAboveTreelineNorth)+ ",");
            sb.Append(Convert.ToInt32(OctagonAboveTreelineNorthEast)+ ",");
            sb.Append(Convert.ToInt32(OctagonAboveTreelineNorthWest)+ ",");
            sb.Append(Convert.ToInt32(OctagonAboveTreelineSouth)+ ",");
            sb.Append(Convert.ToInt32(OctagonAboveTreelineSouthEast)+ ",");
            sb.Append(Convert.ToInt32(OctagonAboveTreelineSouthWest)+ ",");
            sb.Append(Convert.ToInt32(OctagonAboveTreelineWest)+ ",");
            sb.Append(Convert.ToInt32(OctagonNearTreelineEast)+ ",");
            sb.Append(Convert.ToInt32(OctagonNearTreelineNorth)+ ",");
            sb.Append(Convert.ToInt32(OctagonNearTreelineNorthEast)+ ",");
            sb.Append(Convert.ToInt32(OctagonNearTreelineNorthWest)+ ",");
            sb.Append(Convert.ToInt32(OctagonNearTreelineSouth)+ ",");
            sb.Append(Convert.ToInt32(OctagonNearTreelineSouthEast)+ ",");
            sb.Append(Convert.ToInt32(OctagonNearTreelineSouthWest)+ ",");
            sb.Append(Convert.ToInt32(OctagonNearTreelineWest)+ ",");
            sb.Append(Convert.ToInt32(OctagonBelowTreelineEast)+ ",");
            sb.Append(Convert.ToInt32(OctagonBelowTreelineNorth)+ ",");
            sb.Append(Convert.ToInt32(OctagonBelowTreelineNorthEast)+ ",");
            sb.Append(Convert.ToInt32(OctagonBelowTreelineNorthWest)+ ",");
            sb.Append(Convert.ToInt32(OctagonBelowTreelineSouth)+ ",");
            sb.Append(Convert.ToInt32(OctagonBelowTreelineSouthEast)+ ",");
            sb.Append(Convert.ToInt32(OctagonBelowTreelineSouthWest)+ ",");
            sb.Append(Convert.ToInt32(OctagonBelowTreelineWest)+ ",");
            return sb.ToString();    
        }
    }
}