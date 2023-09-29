// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.IO.Compression;

namespace Fluxzy.Readers
{
    public class FxzyImportEngine : IImportEngine
    {
        private readonly FxzyDirectoryPackager _directoryPackager;

        public FxzyImportEngine(FxzyDirectoryPackager directoryPackager)
        {
            _directoryPackager = directoryPackager;
        }

        public bool IsFormat(string fileName)
        {
            try {
                using var zipArchive = ZipFile.Open(fileName, ZipArchiveMode.Read);
                var entry = zipArchive.GetEntry("meta.json");

                return entry != null;
            }
            catch {
                // ignore zip reading error 
                return false;
            }
        }

        public void WriteToDirectory(string fileName, string directory)
        {
            var directoryInfo = new DirectoryInfo(directory);
            var fileInfo = new FileInfo(fileName);

            using var fileStream = fileInfo.OpenRead();

            // Detect format 

            // and unpack 

            _directoryPackager.Unpack(fileStream, directoryInfo.FullName);
        }
    }
}
