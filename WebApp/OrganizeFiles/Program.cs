using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrganizeFiles
{
    class Program
    {
        static void Main(string[] args)
        {
            var source = @"E:\Data\nam-csv-westus-v1\csv\";
            var destinationBase = @"E:\Data\nam-csv-westus-v1.1\";
            var listOfFiles = Directory.GetFiles(source)
                                              .Select(Path.GetFileName)
                                              .ToArray();
            foreach(var f in listOfFiles)
            {
                var endFileName = f;
                if (f.StartsWith("nam_218_201711"))
                {
                    var nameParts = f.Split('_');
                    endFileName = nameParts[2] + "T" + nameParts[4][1] + nameParts[4][2] + "forecastHour00.csv";
                }
                else
                {
                    continue;
                }

                var directoryDate = DateTime.ParseExact(endFileName.Split('T')[0], "yyyyMMdd", null);
                var directoryYear = directoryDate.Year;
                var directoryMonth = directoryDate.Month;
                var directoryDay = directoryDate.Day;

                var destination = destinationBase + directoryYear + @"\" + directoryMonth + @"\" + directoryDay + @"\";
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }
                File.Copy(Path.Combine(source, f), Path.Combine(destination, endFileName), true);
            }
        }
    }
}
