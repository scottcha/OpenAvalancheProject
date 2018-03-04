using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAvalancheProject.Pipeline.Utilities
{
    public static class SnotelUtilities
    {
        public static string CreateSnotelFileDate(DateTime checkDate)
        {
            return checkDate.ToString("yyyyMMdd") + "." + checkDate.ToString("HH");
        }
    }
}
