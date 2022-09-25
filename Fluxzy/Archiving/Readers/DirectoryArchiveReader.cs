// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Fluxzy.Readers
{
    public class DirectoryArchiveReader : IArchiveReader
    {
        private readonly string _baseDirectory;
        private readonly string _captureDirectory;
        private readonly string _contentDirectory;

        public DirectoryArchiveReader(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            _contentDirectory = Path.Combine(baseDirectory, "contents");
            _captureDirectory = Path.Combine(baseDirectory, "captures");
        }

        public IEnumerable<ExchangeInfo> ReadAllExchanges()
        {
            var exchangeDirectory = new DirectoryInfo(Path.Combine(_baseDirectory, "exchanges"));

            return exchangeDirectory.EnumerateFiles("*.json", SearchOption.AllDirectories)
                                    .Select(f =>
                                        JsonSerializer.Deserialize<ExchangeInfo>(
                                            File.ReadAllText(f.FullName), GlobalArchiveOption.JsonSerializerOptions))
                                    .Where(t => t != null)
                                    .Select(t => t!);
        }

        public ExchangeInfo? ReadExchange(int exchangeId)
        {
            var exchangePath = DirectoryArchiveHelper.GetExchangePath(_baseDirectory, exchangeId);

            if (!File.Exists(exchangePath))
                return null;

            return JsonSerializer.Deserialize<ExchangeInfo>(File.ReadAllText(exchangePath),
                GlobalArchiveOption.JsonSerializerOptions);
        }

        public IEnumerable<ConnectionInfo> ReadAllConnections()
        {
            var connectionDirectory = new DirectoryInfo(Path.Combine(_baseDirectory, "connections"));

            return connectionDirectory.EnumerateFiles("*.json", SearchOption.AllDirectories)
                                      .Select(f =>
                                          JsonSerializer.Deserialize<ConnectionInfo>(
                                              File.ReadAllText(f.FullName), GlobalArchiveOption.JsonSerializerOptions))
                                      .Where(t => t != null)
                                      .Select(t => t!);
        }

        public ConnectionInfo? ReadConnection(int connectionId)
        {
            var connectionPath = DirectoryArchiveHelper.GetConnectionPath(_baseDirectory, connectionId);

            if (!File.Exists(connectionPath))
                return null;

            return JsonSerializer.Deserialize<ConnectionInfo>(File.ReadAllText(connectionPath),
                GlobalArchiveOption.JsonSerializerOptions);
        }

        public Stream? GetRawCaptureStream(int connectionId)
        {
            var capturePath = Path.Combine(_captureDirectory, $"{connectionId}.pcap");

            if (!File.Exists(capturePath))
                return null;

            return File.Open(capturePath, FileMode.Open);
        }

        public Stream GetRequestBody(int exchangeId)
        {
            var requestBodyPath = DirectoryArchiveHelper.GetContentRequestPath(_baseDirectory, exchangeId);

            if (!File.Exists(requestBodyPath))
                return null;

            return File.Open(requestBodyPath, FileMode.Open);
        }
        public Stream GetResponseBody(int exchangeId)
        {
            var requestContentPath = DirectoryArchiveHelper.GetContentResponsePath(_baseDirectory, exchangeId);

            if (!File.Exists(requestContentPath))
                return null;

            return File.Open(requestContentPath, FileMode.Open);
        }
    }
}