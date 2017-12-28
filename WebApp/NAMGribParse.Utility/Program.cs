using Grib.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAvalancheProject.Pipeline;
using OpenAvalancheProject.Pipeline.Utilities;

namespace NAMGribParse.Utility
{
    
    class Program
    {
        static void Main(string[] args)
        {

            //using (GribFile file = new GribFile("../../SampleFiles/20171030.nam.t00z.awphys00.tm00.grib2"))
            using (GribFile file = new GribFile(@"C:\Users\scott\AppData\Local\Temp\20171106.nam.t00z.awphys00.tm00.grib2"))
                GribUtilities.ParseNamGribFile(file);
        }

  
    }
}
