using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GetNWACData
{
    public static class Utilities
    {
        public static string CleanStringForCSVExport(string stringToClean)
        {
            if(stringToClean == null)
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
