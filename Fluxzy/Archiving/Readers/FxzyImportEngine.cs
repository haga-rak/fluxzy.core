// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;

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
            // TODO : add a better check here 

            return fileName.EndsWith(".fxzy", StringComparison.OrdinalIgnoreCase)
                   || fileName.EndsWith(".fluxzy", StringComparison.OrdinalIgnoreCase);
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
