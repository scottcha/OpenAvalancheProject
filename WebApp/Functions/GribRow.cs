using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAvalancheProject.Pipeline
{
    public class GribRow
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string ParameterName { get; set; }
        public string ParameterShortName { get; set; }
        public DateTime Time { get; set; }
        public DateTime ReferenceTime { get; set; }
        public int Level { get; set; }
        public string TypeOfLevel { get; set; }
        public int StepSize { get; set; }
        public double Value { get; set; }
    }
}
