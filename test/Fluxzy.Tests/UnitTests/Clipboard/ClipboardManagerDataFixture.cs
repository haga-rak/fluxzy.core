// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using Fluxzy.Readers;

namespace Fluxzy.Tests.UnitTests.Clipboard
{
    public class ClipboardManagerDataFixture : IDisposable
    {
        private readonly byte[] _inMemorySample;
        private readonly string _sourceArchiveFullDirectory;

        public ClipboardManagerDataFixture()
        {
            _inMemorySample = File.ReadAllBytes("_Files/Archives/with-request-payload.fxzy");
            
            var sourceArchiveFullDirectory = "Drop/ClipboardManagerDataFixture/SourceArchive";

            if (Directory.Exists(sourceArchiveFullDirectory)) {
                Directory.Delete(sourceArchiveFullDirectory, true);
            }

            ZipHelper.Decompress(new MemoryStream(_inMemorySample), new DirectoryInfo(sourceArchiveFullDirectory));

            _sourceArchiveFullDirectory = sourceArchiveFullDirectory;
        }

        public int CopyExchangeId { get; } = 101;

        public string SessionId { get; } = Guid.NewGuid().ToString();

        public void Dispose()
        {
        }

        public string GetTempArchiveDirectoryWithExistingFiles()
        {
            var destinationDirectory = $"Drop/ClipboardManagerDataFixture/DestinationArchive/{Guid.NewGuid()}";
            ZipHelper.Decompress(new MemoryStream(_inMemorySample), new DirectoryInfo(destinationDirectory));

            return destinationDirectory;
        }

        public IArchiveReader GetArchiveReader(bool packed)
        {
            return packed
                ? new FluxzyArchiveReader(new MemoryStream(_inMemorySample))
                : new DirectoryArchiveReader(_sourceArchiveFullDirectory);
        }
    }
}
