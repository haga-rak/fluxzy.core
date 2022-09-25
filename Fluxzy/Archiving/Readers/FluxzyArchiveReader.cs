// Copyright © 2022 Haga Rakotoharivelo

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

        public IEnumerable<ExchangeInfo> ReadAllExchanges()
        {
            return _zipFile.Entries.Where(e => 
                               e.FullName.StartsWith("exchanges") 
                                && e.FullName.EndsWith(".json"))
                    .Select(s =>
                    {
                        using var stream = s.Open(); 
                        return JsonSerializer.Deserialize<ExchangeInfo>(
                            stream, 
                            GlobalArchiveOption.JsonSerializerOptions);
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
                GlobalArchiveOption.JsonSerializerOptions);
        }

        public IEnumerable<ConnectionInfo> ReadAllConnections()
        {
            return _zipFile.Entries.Where(e =>
                               e.FullName.StartsWith("connections")
                               && e.FullName.EndsWith(".json"))
                           .Select(s =>
                           {
                               using var stream = s.Open();
                               return JsonSerializer.Deserialize<ConnectionInfo>(
                                   stream,
                                   GlobalArchiveOption.JsonSerializerOptions);
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
                GlobalArchiveOption.JsonSerializerOptions);
        }

        public Stream GetRawCaptureStream(int connectionId)
        {
            var path = Path.Combine("captures", $"{connectionId}.pcap").Replace("\\", "/");
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

        public Stream? GetResponseBody(int exchangeId)
        {
            var path = DirectoryArchiveHelper.GetContentResponsePath(string.Empty, exchangeId).Replace("\\", "/");
            var entry = _zipFile.Entries.FirstOrDefault(e => e.FullName == path);

            if (entry == null)
                return null;

            return entry.Open();
        }

        public void Dispose()
        {
            _zipFile?.Dispose();
        }
    }

    
}