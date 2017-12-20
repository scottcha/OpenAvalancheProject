using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenAvalancheProject.Pipeline.Utilities
{
    public static class SnodasUtilities
    {
        public static List<string> UnpackSnodasArchive(string snodasFileToUncompress)
        { 
            using (Stream stream = File.OpenRead(snodasFileToUncompress))
            {
                return UnpackSnodasStream(stream);
            }
        }

        public static List<string> UnpackSnodasStream(Stream snodasStream)
        {
            var files = new List<string>();
            var uncompressedFiles = new List<string>();
            using (var reader = ReaderFactory.Open(snodasStream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        string filePath = Path.Combine(Path.GetTempPath(), reader.Entry.Key);
                        files.Add(filePath);
                        reader.WriteEntryToDirectory(Path.GetTempPath(), new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true,
                        });
                    }
                }
            }
            foreach (var file in files)
            {
                using (Stream stream = File.OpenRead(file))
                using (var reader = ReaderFactory.Open(stream))
                {
                    while (reader.MoveToNextEntry())
                    {
                        string filePath = Path.Combine(Path.GetTempPath(), reader.Entry.Key);
                        uncompressedFiles.Add(filePath);
                        reader.WriteEntryToDirectory(Path.GetTempPath(), new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true,
                        });
                    }
                }
            }
            return uncompressedFiles;
        }

        /// <summary>
        /// Snodas OGR HDR files have too many bard codes for OGR to be able to read them (I have no idea why)
        /// We need to truncate these.
        /// </summary>
        /// <param name="filePath">Path to the Hdr file</param>
        /// <param name="outFilePath">Optional out file path; if not specified original file will be overwritten</param>
        public static void RemoveBardCodesFromHdr(string filePath, string outFilePath = null)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".hdr")
            {
                throw new ArgumentException($"filePath parameter has invalid extension as {extension} but expected .hdr");
            }

            string outContents = "";

            using (Stream stream = File.Open(filePath, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                var contents = reader.ReadToEnd();
                string pattern = @"[0-9]+ BARD codes:[0-9 ]+";
                Regex regex = new Regex(pattern);
                outContents = regex.Replace(contents, "0");
            }

            if(outFilePath == null)
            {
                outFilePath = filePath;
            }

            using (Stream stream = File.Open(outFilePath, FileMode.OpenOrCreate))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(outContents);
            }
        }
    }
}
