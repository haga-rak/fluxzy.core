// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Har
{
    [PackagerInformation("har", "HAR 1.2 archive format", ".har")]
    public class HttpArchivePackager : DirectoryPackager
    {
        private readonly HttpArchiveSavingSetting _savingSetting;

        public HttpArchivePackager(HttpArchiveSavingSetting? savingSetting = null)
        {
            _savingSetting = savingSetting ?? HttpArchiveSavingSetting.Default;
        }

        public override bool ShouldApplyTo(string fileName)
        {
            return fileName.EndsWith(".har", StringComparison.CurrentCultureIgnoreCase);
        }

        public override Task Pack(string directory, Stream outputStream, HashSet<int>? exchangeIds)
        {
            var baseDirectory = new DirectoryInfo(directory);
            var packableFiles = GetPackableFileInfos(baseDirectory, exchangeIds).ToList();

            var exchanges = ReadExchanges(packableFiles);
            var connections = ReadConnections(packableFiles);

            return InternalPack(directory, outputStream, exchanges, connections);
        }

        private Task InternalPack(
            string directory, Stream outputStream,
            IEnumerable<ExchangeInfo> exchangeInfos,
            IEnumerable<ConnectionInfo> connectionInfos)
        {
            var directoryArchiveReader = new DirectoryArchiveReader(directory);

            var harLogModel = new HarLogModel(directoryArchiveReader, exchangeInfos,
                connectionInfos.ToDictionary(t => t.Id, t => t), _savingSetting);

            JsonSerializer.Serialize(outputStream, new HarSerializeRootModel(harLogModel),
                GlobalArchiveOption.HttpArchiveSerializerOptions);

            return Task.CompletedTask;
        }
    }
}
