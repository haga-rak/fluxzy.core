// Copyright © 2022 Haga RAKOTOHARIVELO

using Fluxzy.Clients;
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

        public DirectoryArchiveReader(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            _captureDirectory = Path.Combine(baseDirectory, "captures");
        }

        public ArchiveMetaInformation ReadMetaInformation()
        {
            var metaPath = DirectoryArchiveHelper.GetMetaPath(_baseDirectory);

            if (!File.Exists(metaPath))
                return new ArchiveMetaInformation();

            using var metaStream = File.Open(metaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            return JsonSerializer.Deserialize<ArchiveMetaInformation>(metaStream,
                GlobalArchiveOption.DefaultSerializerOptions)!;
        }

        public IEnumerable<ExchangeInfo> ReadAllExchanges()
        {
            var exchangeDirectory = new DirectoryInfo(Path.Combine(_baseDirectory, "exchanges"));

            return exchangeDirectory.EnumerateFiles("*.json", SearchOption.AllDirectories)
                                    .Select(f =>
                                        JsonSerializer.Deserialize<ExchangeInfo>(
                                            File.ReadAllText(f.FullName), GlobalArchiveOption.DefaultSerializerOptions))
                                    .Where(t => t != null)
                                    .Select(t => t!);
        }

        public ExchangeInfo? ReadExchange(int exchangeId)
        {
            var exchangePath = DirectoryArchiveHelper.GetExchangePath(_baseDirectory, exchangeId);

            if (!File.Exists(exchangePath))
                return null;

            return JsonSerializer.Deserialize<ExchangeInfo>(File.ReadAllText(exchangePath),
                GlobalArchiveOption.DefaultSerializerOptions);
        }

        public IEnumerable<ConnectionInfo> ReadAllConnections()
        {
            var connectionDirectory = new DirectoryInfo(Path.Combine(_baseDirectory, "connections"));

            return connectionDirectory.EnumerateFiles("*.json", SearchOption.AllDirectories)
                                      .Select(f =>
                                          JsonSerializer.Deserialize<ConnectionInfo>(
                                              File.ReadAllText(f.FullName), GlobalArchiveOption.DefaultSerializerOptions))
                                      .Where(t => t != null)
                                      .Select(t => t!);
        }

        public ConnectionInfo? ReadConnection(int connectionId)
        {
            var connectionPath = DirectoryArchiveHelper.GetConnectionPath(_baseDirectory, connectionId);

            if (!File.Exists(connectionPath))
                return null;

            return JsonSerializer.Deserialize<ConnectionInfo>(File.ReadAllText(connectionPath),
                GlobalArchiveOption.DefaultSerializerOptions);
        }

        public Stream? GetRawCaptureStream(int connectionId)
        {
            var capturePath = Path.Combine(_captureDirectory, $"{connectionId}.pcapng");

            if (!File.Exists(capturePath))
                return null;

            return File.Open(capturePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream? GetRawCaptureKeyStream(int connectionId)
        {
            var capturePath = Path.Combine(_captureDirectory, $"{connectionId}.nsskeylog");

            if (!File.Exists(capturePath))
                return null;

            return File.Open(capturePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public string? GetRawCaptureFile(int connectionId)
        {
            var capturePath = Path.Combine(_captureDirectory, $"{connectionId}.pcapng");

            if (!File.Exists(capturePath))
                return null;

            return capturePath;
        }

        public Stream? GetRequestBody(int exchangeId)
        {
            var requestBodyPath = DirectoryArchiveHelper.GetContentRequestPath(_baseDirectory, exchangeId);

            if (!File.Exists(requestBodyPath))
                return null;

            return File.Open(requestBodyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public long GetRequestBodyLength(int exchangeId)
        {
            var requestBodyPath = DirectoryArchiveHelper.GetContentRequestPath(_baseDirectory, exchangeId);
            var fileInfo = new FileInfo(requestBodyPath);

            if (!fileInfo.Exists)
                return -1;

            return fileInfo.Length; 
        }

        public long GetResponseBodyLength(int exchangeId)
        {
            var responseBodyPath = DirectoryArchiveHelper.GetContentResponsePath(_baseDirectory, exchangeId);
            var fileInfo = new FileInfo(responseBodyPath);

            if (!fileInfo.Exists)
                return 0;

            return fileInfo.Length;
        }

        public Stream? GetRequestWebsocketContent(int exchangeId, int messageId)
        {
            var requestBodyPath =
                DirectoryArchiveHelper.GetWebsocketContentRequestPath(_baseDirectory, exchangeId, messageId);

            if (!File.Exists(requestBodyPath))
                return null;

            return File.Open(requestBodyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream? GetResponseWebsocketContent(int exchangeId, int messageId)
        {
            var responseBodyPath =
                DirectoryArchiveHelper.GetWebsocketContentResponsePath(_baseDirectory, exchangeId, messageId);

            if (!File.Exists(responseBodyPath))
                return null;

            return File.Open(responseBodyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public bool HasRequestBody(int exchangeId)
        {
            var fileInfo = new FileInfo(DirectoryArchiveHelper.GetContentRequestPath(_baseDirectory, exchangeId));

            return fileInfo.Exists && fileInfo.Length > 0;
        }

        public Stream? GetResponseBody(int exchangeId)
        {
            var requestContentPath = DirectoryArchiveHelper.GetContentResponsePath(_baseDirectory, exchangeId);

            if (!File.Exists(requestContentPath))
                return null;

            return File.Open(requestContentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public bool HasResponseBody(int exchangeId)
        {
            var fileInfo = new FileInfo(DirectoryArchiveHelper.GetContentResponsePath(_baseDirectory, exchangeId));

            return fileInfo.Exists && fileInfo.Length > 0;
        }

        public bool HasCapture(int connectionId)
        {
            var fileInfo = new FileInfo(DirectoryArchiveHelper.GetCapturePath(_baseDirectory, connectionId));
            return fileInfo.Exists && fileInfo.Length > 0;
        }

        public void Dispose()
        {
        }
    }
}
