using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetData
{
    public class CAJsonParser : IParser
    {
        private static string MapRatings(string jsonRating)
        {
            switch(jsonRating)
            {
                case "1:Low":
                    return "Low";
                case "2:Moderate":
                    return "Moderate";
                case "3:Considerable":
                    return "Considerable";
                case "4:High":
                    return "High";
                case "5:Extreme":
                    return "Extreme";
                default:
                    return "no-data";
            }
        }

        private static string MapLikelihood(string jsonLikelihood)
        {
            switch(jsonLikelihood)
            {
                case "Unlikely":
                case "Improbable":
                    return "0-unlikely";
                case "Possible":
                case "Possible - Unlikely":
                case "Possible - Improbable":
                    return "1-possible";
                case "Probable":
                case "Probable - Possible":
                case "Likely":
                case "Likely - Possible":
                    return "2-likely";
                case "Very Likely":
                case "Certain - Very Likely":
                case "Very Likely - Likely":
                    return "3-very likely";
                case "Certain":
                    return "4-certain";
                default:
                    return "Unknown Likelihood: " + jsonLikelihood;
            }
        }

        public static string MapSizeValues(string jsonSize)
        {
            //possible values
            //0-small
            //1-large
            //2-very large
            //3-historic 
            switch (jsonSize)
            {
                case "0.5":
                case "1":
                case "1.5":
                    return "0-small";
                case "2":
                case "2.5":
                case "3":
                    return "1-large";
                case "3.5":
                case "4":
                    return "2-very large";
                case "4.5":
                case "5":
                    return "3-historic";
                default:
                    return "no-data";
            }
        }

        public AvalancheRegionForecast Parse(TextReader reader)
        {
            var resultToParse = reader.ReadToEnd();
            dynamic contents = JObject.Parse(resultToParse);
            if(contents == null || contents.dangerRatings.Count == 0)
            {
                throw new Exception("Json didn't parse any objects.");
            }

            var forecast = new AvalancheRegionForecast();
            forecast.Zone = contents.region.Value;
            forecast.PublishDate = contents.dateIssued; 
            forecast.BottomLineSummary = contents.highlights;
            forecast.ConfidenceLevel = contents.confidence;
            var ratings = new List<(DateTime validDate, string elevation, string value)>();
            foreach(var f in contents.dangerRatings)
            {
                ratings.Add((((DateTime)f.date).Date, "alp", MapRatings(f.dangerRating.alp.Value)));
                ratings.Add((((DateTime)f.date).Date, "tln", MapRatings(f.dangerRating.tln.Value)));
                ratings.Add((((DateTime)f.date).Date, "btl", MapRatings(f.dangerRating.btl.Value)));
            }
            var alpineRatings = ratings.Where(r => r.elevation == "alp").OrderBy(r => r.validDate).ToList();
            var treelineRatings = ratings.Where(r => r.elevation == "tln").OrderBy(r => r.validDate).ToList();
            var belowRatings = ratings.Where(r => r.elevation == "btl").OrderBy(r => r.validDate).ToList();

            forecast.Day1Date = alpineRatings[0].validDate;
            forecast.Day1DangerElevationHigh = alpineRatings[0].value;
            forecast.Day1DangerElevationMiddle = treelineRatings[0].value;
            forecast.Day1DangerElevationLow = belowRatings[0].value;    
            forecast.Day2DangerElevationHigh = alpineRatings[1].value;
            forecast.Day2DangerElevationMiddle = treelineRatings[1].value;
            forecast.Day2DangerElevationLow = belowRatings[1].value;    
            forecast.Day3DangerElevationHigh = alpineRatings[2].value;
            forecast.Day3DangerElevationMiddle = treelineRatings[2].value;
            forecast.Day3DangerElevationLow = belowRatings[2].value;

            var problems = contents.problems;
            if(problems != null && problems.Count > 0)
            {
                foreach(var p in problems)
                {
                    var problem = new AvalancheProblem();
                    problem.Likelihood = MapLikelihood(p.likelihood.Value);
                    problem.ProblemName = p.type.Value;
                    problem.MinimumSize = MapSizeValues(p.expectedSize.min.Value);
                    problem.MaximumSize = MapSizeValues(p.expectedSize.max.Value);

                    var aspects = p.aspects;
                    var elevations = p.elevations;
                    if(aspects != null && elevations != null)
                    {
                        foreach(var aspect in aspects)
                        {
                            foreach(var elevation in elevations)
                            {
                                var e = elevation.Value;
                                switch(aspect.Value)
                                {
                                    case "N":
                                    {
                                        if(e == "Alp")
                                        {
                                            problem.OctagonAboveTreelineNorth = true;
                                        }
                                        else if(e == "Tln")
                                        {
                                            problem.OctagonNearTreelineNorth = true;
                                        }
                                        else if(e == "Btl")
                                        {
                                            problem.OctagonBelowTreelineNorth = true;
                                        }
                                        break;
                                    }
                                    case "NE":
                                    {
                                        if(e == "Alp")
                                        {
                                            problem.OctagonAboveTreelineNorthEast = true;
                                        }
                                        else if(e == "Tln")
                                        {
                                            problem.OctagonNearTreelineNorthEast = true;
                                        }
                                        else if(e == "Btl")
                                        {
                                            problem.OctagonBelowTreelineNorthEast = true;
                                        }
                                        break;
                                    }
                                    case "E":
                                    {
                                        if(e == "Alp")
                                        {
                                            problem.OctagonAboveTreelineEast = true;
                                        }
                                        else if(e == "Tln")
                                        {
                                            problem.OctagonNearTreelineEast = true;
                                        }
                                        else if(e == "Btl")
                                        {
                                            problem.OctagonBelowTreelineEast = true;
                                        }
                                        break;
                                    }
                                    case "SE":
                                    {
                                        if(e == "Alp")
                                        {
                                            problem.OctagonAboveTreelineSouthEast = true;
                                        }
                                        else if(e == "Tln")
                                        {
                                            problem.OctagonNearTreelineSouthEast = true;
                                        }
                                        else if(e == "Btl")
                                        {
                                            problem.OctagonBelowTreelineSouthEast = true;
                                        }
                                        break;
                                    }
                                    case "S":
                                    {
                                        if(e == "Alp")
                                        {
                                            problem.OctagonAboveTreelineSouth = true;
                                        }
                                        else if(e == "Tln")
                                        {
                                            problem.OctagonNearTreelineSouth = true;
                                        }
                                        else if(e == "Btl")
                                        {
                                            problem.OctagonBelowTreelineSouth = true;
                                        }
                                        break;
                                    }
                                    case "SW":
                                    {
                                        if(e == "Alp")
                                        {
                                            problem.OctagonAboveTreelineSouthWest = true;
                                        }
                                        else if(e == "Tln")
                                        {
                                            problem.OctagonNearTreelineSouthWest = true;
                                        }
                                        else if(e == "Btl")
                                        {
                                            problem.OctagonBelowTreelineSouthWest = true;
                                        }
                                        break;
                                    }
                                    case "W":
                                    {
                                        if(e == "Alp")
                                        {
                                            problem.OctagonAboveTreelineWest = true;
                                        }
                                        else if(e == "Tln")
                                        {
                                            problem.OctagonNearTreelineWest = true;
                                        }
                                        else if(e == "Btl")
                                        {
                                            problem.OctagonBelowTreelineWest = true;
                                        }
                                        break;
                                    }
                                    case "NW":
                                    {
                                        if(e == "Alp")
                                        {
                                            problem.OctagonAboveTreelineNorthWest = true;
                                        }
                                        else if(e == "Tln")
                                        {
                                            problem.OctagonNearTreelineNorthWest = true;
                                        }
                                        else if(e == "Btl")
                                        {
                                            problem.OctagonBelowTreelineNorthWest = true;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    forecast.AvalancheProblems.Add(problem);
                }
            }
            return forecast;
        }
    }
}
