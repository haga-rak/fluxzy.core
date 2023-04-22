// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Fluxzy.Har;
using Fluxzy.Readers;
using Fluxzy.Tests._Files;

namespace Fluxzy.Tests.Archiving.Fixtures
{
    public class HarFileFixture : IDisposable
    {
        private readonly string _tempDirectory;

        public HarFileFixture()
        {
            _tempDirectory = nameof(HarFileFixture) + Guid.NewGuid();

            using (var zipArchive = new ZipArchive(new MemoryStream(StorageContext.multipart_request_fxzy))) {
                zipArchive.ExtractToDirectory(_tempDirectory);
            }

            var directoryReader = new DirectoryArchiveReader(_tempDirectory);

            Exchanges = directoryReader.ReadAllExchanges().ToList();

            var httpArchivePackager = new HttpArchivePackager();
            using var memoryStream = new MemoryStream();

            httpArchivePackager.Pack(_tempDirectory, memoryStream, null).GetAwaiter().GetResult();

            memoryStream.Seek(0, SeekOrigin.Begin);

            Document = JsonDocument.Parse(memoryStream);
        }

        public JsonDocument Document { get; }

        public List<ExchangeInfo> Exchanges { get; }

        public void Dispose()
        {
            Document.Dispose();

            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, true);
        }
    }
}
