// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace Fluxzy.Readers
{
    public class FluxzyArchiveReader : IArchiveReader
    {
        private readonly ZipArchive _zipFile;

        public FluxzyArchiveReader(string filePath)
        {
            _zipFile = ZipFile.OpenRead(filePath);
        }

        public FluxzyArchiveReader(Stream stream)
        {
            _zipFile = new ZipArchive(stream, ZipArchiveMode.Read);
        }

        public ArchiveMetaInformation ReadMetaInformation()
        {
            var metaEntry = _zipFile.Entries.FirstOrDefault(e => e.FullName.EndsWith("meta.json"));

            if (metaEntry == null)
                return new ArchiveMetaInformation();

            using var metaStream = metaEntry.Open();

            return JsonSerializer.Deserialize<ArchiveMetaInformation>(metaStream,
                GlobalArchiveOption.DefaultSerializerOptions)!;
        }

        public IEnumerable<ExchangeInfo> ReadAllExchanges()
        {
            return _zipFile.Entries.Where(e =>
                               e.FullName.StartsWith("exchanges")
                               && e.FullName.EndsWith(".json"))
                           .Select(s => {
                               using var stream = s.Open();

                               return JsonSerializer.Deserialize<ExchangeInfo>(
                                   stream,
                                   GlobalArchiveOption.DefaultSerializerOptions);
                           })
                           .Where(t => t != null)
                           .Select(t => t!);
        }

        public ExchangeInfo? ReadExchange(int exchangeId)
        {
            var path = DirectoryArchiveHelper.GetExchangePath(string.Empty, exchangeId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return null;

            using var stream = entry.Open();

            return JsonSerializer.Deserialize<ExchangeInfo>(
                stream,
                GlobalArchiveOption.DefaultSerializerOptions);
        }

        public IEnumerable<ConnectionInfo> ReadAllConnections()
        {
            return _zipFile.Entries.Where(e =>
                               e.FullName.StartsWith("connections")
                               && e.FullName.EndsWith(".json"))
                           .Select(s => {
                               using var stream = s.Open();

                               return JsonSerializer.Deserialize<ConnectionInfo>(
                                   stream,
                                   GlobalArchiveOption.DefaultSerializerOptions);
                           })
                           .Where(t => t != null)
                           .Select(t => t!);
        }

        public ConnectionInfo? ReadConnection(int connectionId)
        {
            var path = DirectoryArchiveHelper.GetConnectionPath(string.Empty, connectionId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return null;

            using var stream = entry.Open();

            return JsonSerializer.Deserialize<ConnectionInfo>(
                stream,
                GlobalArchiveOption.DefaultSerializerOptions);
        }

        public Stream? GetRawCaptureStream(int connectionId)
        {
            var path = Path.Combine("captures", $"{connectionId}.pcapng").Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return null;

            return entry.Open();
        }

        public Stream? GetRawCaptureKeyStream(int connectionId)
        {
            var path = Path.Combine("captures", $"{connectionId}.nsskeylog").Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return null;

            return entry.Open();
        }

        public Stream? GetRequestBody(int exchangeId)
        {
            var path = DirectoryArchiveHelper.GetContentRequestPath(string.Empty, exchangeId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return null;

            return entry.Open();
        }

        public long GetRequestBodyLength(int exchangeId)
        {
            var path = DirectoryArchiveHelper.GetContentRequestPath(string.Empty, exchangeId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return -1;

            return entry.Length;
        }

        public long GetResponseBodyLength(int exchangeId)
        {
            var path = DirectoryArchiveHelper.GetContentResponsePath(string.Empty, exchangeId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return 0;

            return entry.Length;
        }

        public Stream? GetRequestWebsocketContent(int exchangeId, int messageId)
        {
            var path = DirectoryArchiveHelper.GetWebsocketContentRequestPath(string.Empty, exchangeId, messageId)
                                             .Replace("\\", "/");

            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return null;

            return entry.Open();
        }

        public Stream? GetResponseWebsocketContent(int exchangeId, int messageId)
        {
            var path = DirectoryArchiveHelper.GetWebsocketContentResponsePath(string.Empty, exchangeId, messageId)
                                             .Replace("\\", "/");

            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return null;

            return entry.Open();
        }

        public bool HasRequestBody(int exchangeId)
        {
            var path = DirectoryArchiveHelper.GetContentRequestPath(string.Empty, exchangeId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            return entry != null;
        }

        public Stream? GetResponseBody(int exchangeId)
        {
            var path = DirectoryArchiveHelper.GetContentResponsePath(string.Empty, exchangeId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return null;

            return entry.Open();
        }

        public bool HasResponseBody(int exchangeId)
        {
            var path = DirectoryArchiveHelper.GetContentResponsePath(string.Empty, exchangeId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            return entry != null;
        }

        public bool HasCapture(int connectionId)
        {
            var path = DirectoryArchiveHelper.GetCapturePath(string.Empty, connectionId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            return entry != null;
        }

        public void Dispose()
        {
            _zipFile?.Dispose();
        }
    }
}
