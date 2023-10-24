using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetData
{
    public static class Utilities
    {
        public static string CleanStringForCSVExport(string stringToClean)
        {
            if (stringToClean == null)
            {
                return "";
            }
            var result = stringToClean.Replace(',', ' ')
                                      .Replace('"', ' ')
                                      .Replace('\n', ' ')
                                      .Replace('\t', ' ')
                                      .Replace('\r', ' ');
            return result;

        }
    }
}
