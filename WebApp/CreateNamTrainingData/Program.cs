using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grib.Api;
using OpenAvalancheProject.Pipeline;
using OpenAvalancheProject.Pipeline.Utilities;
namespace CreateNamTrainingData
{
    class Program
    {
        static void Main(string[] args)
        {
            GribEnvironment.Init();
#if DEBUG == false
            GribEnvironment.DefinitionsPath = @"D:\home\site\wwwroot\bin\Grib.Api\definitions";
#endif
            var listOfUnpackedFiles = Directory.GetFiles(@"E:\Data\RawWeatherData\nam\smallTraining\")
                                               .Select(Path.GetFileName)
                                               .ToArray();
            var listOfProcessedFiles = Directory.GetFiles(@"E:\Data\nam-csv-westus-v1\csv\")
                                                .Select(Path.GetFileNameWithoutExtension)
                                                .ToArray();
            var filteredListOfUnpackedFiles = listOfUnpackedFiles.Except(listOfProcessedFiles).ToArray();
            Parallel.ForEach(filteredListOfUnpackedFiles, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (f) =>
            {
                string localFileName = @"E:\Data\RawWeatherData\nam\smallTraining\" + f;

                var rowList = new List<NamTableRow>();

                //2. Get values from file
                using (GribFile file = new GribFile(localFileName))
                {
                    rowList = GribUtilities.ParseNamGribFile(file);
                }

                using (FileStream s = new FileStream(@"E:\Data\nam-csv-westus-v1\csv\" + localFileName.Split('\\')[5] + ".csv", FileMode.OpenOrCreate))
                using (StreamWriter csvWriter = new StreamWriter(s, Encoding.UTF8))
                {
                    csvWriter.WriteLine(NamTableRow.Columns);

                    foreach (var row in rowList)
                    {
                        csvWriter.WriteLine(row.ToString());
                    }
                    csvWriter.Flush();
                }
            });
        }
    }
}
