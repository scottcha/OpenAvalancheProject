using System.Xml;

namespace GetCAData
{
    public class ParseCAAML
    {
        public ParseCAAML(string xml)
        {
            var forecast = new XmlDocument();
            forecast.LoadXml(xml);
            var ratingsXPath = "/CaamlData[@xmlns:gml=\"http://www.opengis.net/gml\"]/observations/"; //Bulletin[@gml:id=\"*\"]/bulletinResultsOf/BulletinMeasurements/dangerRatings";
            var ratingsNode = forecast.SelectSingleNode(ratingsXPath);

            if(ratingsNode != null)
            {
                foreach(XmlNode rating in ratingsNode)
                {
                    var time = rating.SelectSingleNode("/validTime/TimeInstant/timePosition");
                    var danger = rating.SelectSingleNode("/DangerRating[1]/customData/DangerRatingDisplay/mainLabel");
                }
            }
            else
            {
                throw new Exception("Have unexpected null node.");
            }
        }

    }

}