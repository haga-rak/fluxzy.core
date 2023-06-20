// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fluxzy.Tests._Fixtures
{
    public abstract class ProduceDeletableItem : IDisposable
    {
        private readonly List<FileSystemInfo> _registeredFileSystemInfos = new();

        protected bool DisablePurge { get; set; }

        public void Dispose()
        {
            if (DisablePurge)
                return;

            foreach (var fileSystemInfo in ((IEnumerable<FileSystemInfo>) _registeredFileSystemInfos).Reverse()) {
                try {
                    if (fileSystemInfo is FileInfo fileInfo) {
                        if (fileInfo.Exists)
                            fileInfo.Delete();
                    }
                    else if (fileSystemInfo is DirectoryInfo directoryInfo) {
                        if (directoryInfo.Exists)
                            directoryInfo.Delete(true);
                    }
                }
                catch {
                    // Ignore deletion errors
                }
            }
        }

        protected string GetRegisteredRandomFile()
        {
            var tempFileName = $"{nameof(ProduceDeletableItem)}/{Guid.NewGuid()}";

            return RegisterFile(tempFileName);
        }

        protected string RegisterFile(string fileName, bool createParentDirectory = true)
        {
            var fileInfo = new FileInfo(fileName);

            _registeredFileSystemInfos.Add(fileInfo);

            if (createParentDirectory)
                fileInfo.Directory?.Create();

            return fileName;
        }

        protected string RegisterDirectory(string directoryName, bool create = true)
        {
            var directoryInfo = new DirectoryInfo(directoryName);
            _registeredFileSystemInfos.Add(directoryInfo);

            if (create)
                directoryInfo.Create();

            return directoryName;
        }
    }
}
