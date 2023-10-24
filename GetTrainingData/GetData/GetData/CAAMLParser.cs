using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace GetData
{
    public class CAAMLParser : IParser
    {
        private CaamlDataType cObj;
        public static string MapLikelihoodOfTriggeringValueType(LikelihoodOfTriggeringValueType toMap)
        {
            //possible values
            //0-unlikely
            //1-possible
            //2-likely
            //3-very likely
            //no-data
            switch (toMap)
            {
                case LikelihoodOfTriggeringValueType.Probable:
                    return "2-likely";
                case LikelihoodOfTriggeringValueType.ProbablePossible:
                    return "2-likely";
                case LikelihoodOfTriggeringValueType.LikelyPossible:
                    return "2-likely";
                case LikelihoodOfTriggeringValueType.Likely:
                    return "2-likely";
                case LikelihoodOfTriggeringValueType.Unlikely:
                    return "0-unlikely";
                case LikelihoodOfTriggeringValueType.Certain:
                    return "3-very likely";
                case LikelihoodOfTriggeringValueType.CertainVeryLikely:
                    return "3-very likely";
                case LikelihoodOfTriggeringValueType.Improbable:
                    return "0-unlikely";
                case LikelihoodOfTriggeringValueType.Possible:
                    return "1-possible";
                case LikelihoodOfTriggeringValueType.PossibleImprobable:
                    return "1-possible";
                case LikelihoodOfTriggeringValueType.PossibleUnlikely:
                    return "1-possible";
                case LikelihoodOfTriggeringValueType.VeryLikely:
                    return "3-very likely";
                case LikelihoodOfTriggeringValueType.VeryLikelyLikely:
                    return "3-very likely";
                default:
                    return "Unknown value " + toMap.ToString();
            }
        }

        public static string MapSizeValues(ExpectedAvSizeValueType toMap)
        {
            //possible values
            //0-small
            //1-large
            //2-very large
            //3-historic 
            switch (toMap)
            {
                case ExpectedAvSizeValueType.Item05:
                case ExpectedAvSizeValueType.Item10:
                case ExpectedAvSizeValueType.Item15:
                    return "0-small";
                case ExpectedAvSizeValueType.Item20:
                case ExpectedAvSizeValueType.Item25:
                case ExpectedAvSizeValueType.Item30:
                    return "1-large";
                case ExpectedAvSizeValueType.Item35:
                case ExpectedAvSizeValueType.Item40:
                    return "2-very large";
                case ExpectedAvSizeValueType.Item45:
                case ExpectedAvSizeValueType.Item50:
                    return "3-historic";
                default:
                    return "Unknown value " + toMap.ToString();
            }
        }

        public AvalancheRegionForecast Parse(TextReader reader)
        {
            var serializer = new XmlSerializer(typeof(CaamlDataType));
            var obj = serializer.Deserialize(reader);
            if (obj == null)
                throw new Exception("Couldn't deserialize xml");
            cObj = (CaamlDataType)obj;

            var bulletin = cObj.observations.Item as BulletinType;
            if (bulletin == null)
                throw new Exception("Didn't find Bulletin in xml");

            var forecastTime = DateTime.Parse(((TimePeriodType)bulletin.validTime.Item).beginPosition.Value);
            var ratings = new List<(DateTime validDate, string elevation, string value)>();
            foreach (var rating in bulletin.bulletinResultsOf.BulletinMeasurements.dangerRatings)
            {
                //TODO: The display values for ratings are defined as a sperate xsd type, 
                //I'm not sure exactly how to cast that to the correct type, for now i'll just
                //do the mapping manually
                var ratingValue = "Unknown";
                switch (rating.mainValue)
                {
                    case "1":
                        ratingValue = "Low";
                        break;
                    case "2":
                        ratingValue = "Moderate";
                        break;
                    case "3":
                        ratingValue = "Considerable";
                        break;
                    case "4":
                        ratingValue = "High";
                        break;
                    case "5":
                        ratingValue = "Extreme";
                        break;
                    case "N/A":
                        ratingValue = "no-data";
                        break;
                    default:
                        ratingValue = rating.mainValue + ":Unknown";
                        break;
                }
                //Get time of rating
                var timeNode = (TimeInstantType)rating.validTime.Item;
                var t = DateTime.Parse(timeNode.timePosition.Value);

                var elevation = "Unknown Elevation";
                switch (rating.validElevation.href)
                {
                    case "ElevationLabel_Alp":
                        elevation = "AboveTreeline";
                        break;
                    case "ElevationLabel_Tln":
                        elevation = "NearTreeline";
                        break;
                    case "ElevationLabel_Btl":
                        elevation = "BelowTreeline";
                        break;
                }
                ratings.Add((validDate: t, elevation: elevation, value: ratingValue));
            }

            //now we can transform it in to our schema
            var forecast = new AvalancheRegionForecast();
            forecast.Zone = ((RegionType)bulletin.locRef.Item).name;
            forecast.PublishDate = forecastTime;
            forecast.ResourceUri = cObj.metaDataProperty.MetaData.srcURL;
            forecast.BottomLineSummary = bulletin.bulletinResultsOf.BulletinMeasurements.highlights;
            forecast.Day1Date = ratings.First().validDate;
            forecast.ConfidenceLevel = bulletin.bulletinResultsOf.BulletinMeasurements.bulletinConfidence.Components.confidenceLevel;
            var AboveForecasts = ratings.Where(r => r.elevation == "AboveTreeline").OrderBy(r => r.validDate).ToList();
            var NearForecasts = ratings.Where(r => r.elevation == "NearTreeline").OrderBy(r => r.validDate).ToList();
            var BelowForecasts = ratings.Where(r => r.elevation == "BelowTreeline").OrderBy(r => r.validDate).ToList();
            Debug.Assert(AboveForecasts.Count == 3);
            Debug.Assert(NearForecasts.Count == 3);
            Debug.Assert(BelowForecasts.Count == 3);
            forecast.Day1DangerElevationHigh = AboveForecasts[0].value;
            forecast.Day2DangerElevationHigh = AboveForecasts[1].value;
            forecast.Day3DangerElevationHigh = AboveForecasts[2].value;
            forecast.Day1DangerElevationMiddle = NearForecasts[0].value;
            forecast.Day2DangerElevationMiddle = NearForecasts[1].value;
            forecast.Day3DangerElevationMiddle = NearForecasts[2].value;
            forecast.Day1DangerElevationLow = BelowForecasts[0].value;
            forecast.Day2DangerElevationLow = BelowForecasts[1].value;
            forecast.Day3DangerElevationLow = BelowForecasts[2].value;

            var problems = bulletin.bulletinResultsOf.BulletinMeasurements.avProblems.AvProblem;
            if (problems != null)
            {
                //Find avy problems
                foreach (var problem in problems)
                {
                    var p = new AvalancheProblem();

                    switch (((AvProblemTypeType)problem.type).ToString())
                    {
                        case "LooseDry":
                        case "DryLoose":
                        case "Dry Loose":
                            p.ProblemName = "Loose Dry";
                            break;
                        case "LooseWet":
                        case "WetLoose":
                        case "Wet Loose":
                            p.ProblemName = "Loose Wet";
                            break;
                        case "WindSlabs":
                            p.ProblemName = "Wind Slabs";
                            break;
                        case "StormSlabs":
                            p.ProblemName = "Storm Slabs";
                            break;
                        case "WetSlabs":
                            p.ProblemName = "Wet Slabs";
                            break;
                        case "PersistentSlabs":
                            p.ProblemName = "Persistent Slabs";
                            break;
                        case "DeepPersistentSlabs":
                            p.ProblemName = "Deep Persistent Slabs";
                            break;
                        case "Cornices":
                            p.ProblemName = "Cornices";
                            break;
                        default:
                            p.ProblemName = "Unknown Problem Type " + ((AvProblemTypeType)problem.type).ToString();
                            break;
                    }

                    p.Likelihood = MapLikelihoodOfTriggeringValueType(problem.likelihoodOfTriggering.Values.typical);
                    p.MaximumSize = MapSizeValues(problem.expectedAvSize.Values.max);
                    p.MinimumSize = MapSizeValues(problem.expectedAvSize.Values.min);

                    foreach (var problemElevation in problem.validElevation)
                    {
                        foreach (var aspect in problem.validAspect)
                        {
                            switch (aspect.href)
                            {
                                case "AspectRange_N":
                                    {
                                        if (problemElevation.href == "ElevationLabel_Alp")
                                        {
                                            p.OctagonAboveTreelineNorth = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Tln")
                                        {
                                            p.OctagonNearTreelineNorth = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Btl")
                                        {
                                            p.OctagonBelowTreelineNorth = true;
                                        }
                                        break;
                                    }
                                case "AspectRange_NE":
                                    {
                                        if (problemElevation.href == "ElevationLabel_Alp")
                                        {
                                            p.OctagonAboveTreelineNorthEast = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Tln")
                                        {
                                            p.OctagonNearTreelineNorthEast = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Btl")
                                        {
                                            p.OctagonBelowTreelineNorthEast = true;
                                        }
                                        break;
                                    }
                                case "AspectRange_E":
                                    {
                                        if (problemElevation.href == "ElevationLabel_Alp")
                                        {
                                            p.OctagonAboveTreelineEast = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Tln")
                                        {
                                            p.OctagonNearTreelineEast = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Btl")
                                        {
                                            p.OctagonBelowTreelineEast = true;
                                        }
                                        break;
                                    }
                                case "AspectRange_SE":
                                    {
                                        if (problemElevation.href == "ElevationLabel_Alp")
                                        {
                                            p.OctagonAboveTreelineSouthEast = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Tln")
                                        {
                                            p.OctagonNearTreelineSouthEast = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Btl")
                                        {
                                            p.OctagonBelowTreelineSouthEast = true;
                                        }
                                        break;
                                    }
                                case "AspectRange_S":
                                    {
                                        if (problemElevation.href == "ElevationLabel_Alp")
                                        {
                                            p.OctagonAboveTreelineSouth = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Tln")
                                        {
                                            p.OctagonNearTreelineSouth = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Btl")
                                        {
                                            p.OctagonBelowTreelineSouth = true;
                                        }
                                        break;
                                    }
                                case "AspectRange_SW":
                                    {
                                        if (problemElevation.href == "ElevationLabel_Alp")
                                        {
                                            p.OctagonAboveTreelineSouthWest = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Tln")
                                        {
                                            p.OctagonNearTreelineSouthWest = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Btl")
                                        {
                                            p.OctagonBelowTreelineSouthWest = true;
                                        }
                                        break;
                                    }
                                case "AspectRange_W":
                                    {
                                        if (problemElevation.href == "ElevationLabel_Alp")
                                        {
                                            p.OctagonAboveTreelineWest = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Tln")
                                        {
                                            p.OctagonNearTreelineWest = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Btl")
                                        {
                                            p.OctagonBelowTreelineWest = true;
                                        }
                                        break;
                                    }
                                case "AspectRange_NW":
                                    {
                                        if (problemElevation.href == "ElevationLabel_Alp")
                                        {
                                            p.OctagonAboveTreelineNorthWest = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Tln")
                                        {
                                            p.OctagonNearTreelineNorthWest = true;
                                        }
                                        else if (problemElevation.href == "ElevationLabel_Btl")
                                        {
                                            p.OctagonBelowTreelineNorthWest = true;
                                        }
                                        break;
                                    }
                                default:
                                    throw new Exception(String.Format("Unxpected problem elevation {0} or aspect {1}", problemElevation.href, aspect.href));

                            }
                        }
                    }
                    forecast.AvalancheProblems.Add(p);
                }
            }
            return forecast;
        }
    }
}