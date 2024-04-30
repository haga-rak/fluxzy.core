// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using Fluxzy.Readers;

namespace Fluxzy.Tests.UnitTests.Clipboard
{
    public class ClipboardManagerDataFixture : IDisposable
    {
        private readonly string _sourceArchiveFullDirectory;

        public ClipboardManagerDataFixture()
        {
            var sourceArchiveFullDirectory = "Drop/ClipboardManagerDataFixture/SourceArchive";

            if (Directory.Exists(sourceArchiveFullDirectory))
                Directory.Delete(sourceArchiveFullDirectory, true);

            var stream = new FileStream(SourceArchiveFullPath, FileMode.Open);
            ZipHelper.Decompress(stream, new DirectoryInfo(sourceArchiveFullDirectory));

            _sourceArchiveFullDirectory = sourceArchiveFullDirectory;
        }

        public string SourceArchiveFullPath { get; } = "_Files/Archives/with-request-payload.fxzy";

        public int CopyExchangeId { get; } = 101;

        public string SessionId { get; } = Guid.NewGuid().ToString();

        public IArchiveReader GetArchiveReader(bool packed)
        {
            return packed? 
                new FluxzyArchiveReader(SourceArchiveFullPath):
                new DirectoryArchiveReader(_sourceArchiveFullDirectory);
        }

        public void Dispose()
        {
        }
    }
}
