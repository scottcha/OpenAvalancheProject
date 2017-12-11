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
