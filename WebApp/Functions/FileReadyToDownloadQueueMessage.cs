using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAvalancheProject.Pipeline
{
    public class FileReadyToDownloadQueueMessage
    {
        public string FileName { get; set; }
        public string FileDate { get; set; }
        public string Url { get; set; }
        public string Filetype { get; set; }
        public string UniqueFileName
        {
            get
            {
                return FileDate + "." + FileName;
            }
        }
    }
}
