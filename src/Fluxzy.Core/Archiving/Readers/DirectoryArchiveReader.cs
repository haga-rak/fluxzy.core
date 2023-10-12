// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fluxzy.Extensions;
using Fluxzy.Misc;
using MessagePack;

namespace Fluxzy.Readers
{
    public class DirectoryArchiveReader : IArchiveReader
    {

        private readonly string _captureDirectory;

        public DirectoryArchiveReader(string baseDirectory)
        {
            BaseDirectory = baseDirectory;
            _captureDirectory = Path.Combine(baseDirectory, "captures");
        }

        public string BaseDirectory { get; }

        public ArchiveMetaInformation ReadMetaInformation()
        {
            var metaPath = DirectoryArchiveHelper.GetMetaPath(BaseDirectory);

            if (!File.Exists(metaPath))
                return new ArchiveMetaInformation();

            using var metaStream = File.Open(metaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            return JsonSerializer.Deserialize<ArchiveMetaInformation>(metaStream,
                GlobalArchiveOption.DefaultSerializerOptions)!;
        }

        public IEnumerable<ExchangeInfo> ReadAllExchanges()
        {
            var exchangeDirectory = new DirectoryInfo(Path.Combine(BaseDirectory, "exchanges"));

            if (!exchangeDirectory.Exists)
                return Enumerable.Empty<ExchangeInfo>();

            return exchangeDirectory.EnumerateFiles("*.mpack", SearchOption.AllDirectories)
                                    .Select(f => {

                                        try {
                                            return MessagePackSerializer.Deserialize<ExchangeInfo>(File.ReadAllBytes(f.FullName),
                                                GlobalArchiveOption.MessagePackSerializerOptions);
                                        }
                                        catch {
                                            // some files may be halfwritten in the proxy was not halted correctly
                                            // ignore
                                            return null; 
                                        }
                                        
                                    })
                                    .Where(t => t != null)
                                    .OrderBy(t => t!.Id)
                                    .Select(t => t!);
        }

        public ExchangeInfo? ReadExchange(int exchangeId)
        {
            var exchangePath = DirectoryArchiveHelper.GetExchangePath(BaseDirectory, exchangeId);

            if (!File.Exists(exchangePath))
                return null;
            
            return MessagePackSerializer.Deserialize<ExchangeInfo>(File.ReadAllBytes(exchangePath),
                GlobalArchiveOption.MessagePackSerializerOptions);
        }

        public IEnumerable<ConnectionInfo> ReadAllConnections()
        {
            var connectionDirectory = new DirectoryInfo(Path.Combine(BaseDirectory, "connections"));

            if (!connectionDirectory.Exists)
                return Enumerable.Empty<ConnectionInfo>();

            return connectionDirectory.EnumerateFiles("*.mpack", SearchOption.AllDirectories)
                                      .Select(f => {

                                          try
                                          {
                                              return MessagePackSerializer.Deserialize<ConnectionInfo>(
                                                  File.ReadAllBytes(f.FullName),
                                                  GlobalArchiveOption.MessagePackSerializerOptions);
                                          }
                                          catch
                                          {
                                              // some files may be halfwritten in the proxy was not halted correctly
                                              // ignore
                                              return null;
                                          }
                                      })
                                      .Where(t => t != null)
                                      .Select(t => t!);
        }

        public IReadOnlyCollection<DownstreamErrorInfo> ReaderAllDownstreamErrors()
        {
            var path = DirectoryArchiveHelper.GetErrorPath(BaseDirectory);

            return MessagePackQueueExtensions.DeserializeMultiple<DownstreamErrorInfo>(path,
                GlobalArchiveOption.MessagePackSerializerOptions);
        }

        public ConnectionInfo? ReadConnection(int connectionId)
        {
            var connectionPath = DirectoryArchiveHelper.GetConnectionPath(BaseDirectory, connectionId);

            if (!File.Exists(connectionPath))
                return null;

            return MessagePackSerializer.Deserialize<ConnectionInfo>(
                File.ReadAllBytes(connectionPath),
                GlobalArchiveOption.MessagePackSerializerOptions);
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

        public Stream? GetRequestBody(int exchangeId)
        {
            var requestBodyPath = DirectoryArchiveHelper.GetContentRequestPath(BaseDirectory, exchangeId);

            if (!File.Exists(requestBodyPath))
                return null;

            return File.Open(requestBodyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream? GetDecodedRequestBody(ExchangeInfo exchangeInfo)
        {
            var originalStream = GetRequestBody(exchangeInfo.Id); 

            if (originalStream == null) 
                return null;

            return exchangeInfo.GetDecodedResponseBodyStream(originalStream, out _);
        }

        public long GetRequestBodyLength(int exchangeId)
        {
            var requestBodyPath = DirectoryArchiveHelper.GetContentRequestPath(BaseDirectory, exchangeId);
            var fileInfo = new FileInfo(requestBodyPath);

            if (!fileInfo.Exists)
                return -1;

            return fileInfo.Length;
        }

        public long GetResponseBodyLength(int exchangeId)
        {
            var responseBodyPath = DirectoryArchiveHelper.GetContentResponsePath(BaseDirectory, exchangeId);
            var fileInfo = new FileInfo(responseBodyPath);

            if (!fileInfo.Exists)
                return 0;

            return fileInfo.Length;
        }

        public Stream? GetRequestWebsocketContent(int exchangeId, int messageId)
        {
            var requestBodyPath =
                DirectoryArchiveHelper.GetWebsocketContentRequestPath(BaseDirectory, exchangeId, messageId);

            if (!File.Exists(requestBodyPath))
                return null;

            return File.Open(requestBodyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream? GetResponseWebsocketContent(int exchangeId, int messageId)
        {
            var responseBodyPath =
                DirectoryArchiveHelper.GetWebsocketContentResponsePath(BaseDirectory, exchangeId, messageId);

            if (!File.Exists(responseBodyPath))
                return null;

            return File.Open(responseBodyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public bool HasRequestBody(int exchangeId)
        {
            var fileInfo = new FileInfo(DirectoryArchiveHelper.GetContentRequestPath(BaseDirectory, exchangeId));

            return fileInfo.Exists && fileInfo.Length > 0;
        }

        public Stream? GetResponseBody(int exchangeId)
        {
            var requestContentPath = DirectoryArchiveHelper.GetContentResponsePath(BaseDirectory, exchangeId);

            if (!File.Exists(requestContentPath))
                return null;

            return File.Open(requestContentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream? GetDecodedResponseBody(ExchangeInfo exchangeInfo)
        {
            var originalStream = GetResponseBody(exchangeInfo.Id);

            if (originalStream == null)
                return null;

            return exchangeInfo.GetDecodedResponseBodyStream(originalStream, out _, true);
        }

        public bool HasResponseBody(int exchangeId)
        {
            var fileInfo = new FileInfo(DirectoryArchiveHelper.GetContentResponsePath(BaseDirectory, exchangeId));

            return fileInfo.Exists && fileInfo.Length > 0;
        }

        public bool HasCapture(int connectionId)
        {
            var fileInfo = new FileInfo(DirectoryArchiveHelper.GetCapturePath(BaseDirectory, connectionId));

            return fileInfo.Exists && fileInfo.Length > 0;
        }

        public void Dispose()
        {
        }

        public string? GetRawCaptureFile(int connectionId)
        {
            var capturePath = Path.Combine(_captureDirectory, $"{connectionId}.pcapng");

            if (!File.Exists(capturePath))
                return null;

            return capturePath;
        }
    }
}
