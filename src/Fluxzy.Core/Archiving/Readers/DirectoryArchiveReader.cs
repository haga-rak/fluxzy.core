// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fluxzy.Extensions;
using Fluxzy.Misc;
using MessagePack;

namespace Fluxzy.Readers
{
    /// <summary>
    ///   An archive reader that read from a directory
    /// </summary>
    public class DirectoryArchiveReader : IArchiveReader
    {
        private readonly string _captureDirectory;
        private readonly ConcurrentDictionary<string, long> _lengthCaching = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseDirectory">The fluxzy directory to read</param>
        /// <param name="enableFileBodyInfoCaching">Caches extended infos about request/response bodies</param>
        public DirectoryArchiveReader(string baseDirectory, bool enableFileBodyInfoCaching = true)
        {
            BaseDirectory = baseDirectory;
            EnableFileBodyInfoCaching = enableFileBodyInfoCaching;
            _captureDirectory = Path.Combine(baseDirectory, "captures");
        }

        /// <summary>
        /// Caches extended infos about request/response bodies
        /// </summary>
        public string BaseDirectory { get; }

        public bool EnableFileBodyInfoCaching { get; }

        public ArchiveMetaInformation ReadMetaInformation()
        {
            var metaPath = DirectoryArchiveHelper.GetMetaPath(BaseDirectory);

            if (!File.Exists(metaPath)) {
                return new ArchiveMetaInformation();
            }

            using var metaStream = File.Open(metaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            return JsonSerializer.Deserialize<ArchiveMetaInformation>(metaStream,
                GlobalArchiveOption.DefaultSerializerOptions)!;
        }

        public IEnumerable<ExchangeInfo> ReadAllExchanges()
        {
            var exchangeDirectory = new DirectoryInfo(Path.Combine(BaseDirectory, "exchanges"));

            if (!exchangeDirectory.Exists) {
                return Enumerable.Empty<ExchangeInfo>();
            }

            return exchangeDirectory.EnumerateFiles("*.mpack", SearchOption.AllDirectories)
                                    .Select(f => {
                                        try {
                                            return MessagePackSerializer.Deserialize<ExchangeInfo>(
                                                File.ReadAllBytes(f.FullName),
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

            if (!File.Exists(exchangePath)) {
                return null;
            }

            return MessagePackSerializer.Deserialize<ExchangeInfo>(File.ReadAllBytes(exchangePath),
                GlobalArchiveOption.MessagePackSerializerOptions);
        }

        public IEnumerable<ConnectionInfo> ReadAllConnections()
        {
            var connectionDirectory = new DirectoryInfo(Path.Combine(BaseDirectory, "connections"));

            if (!connectionDirectory.Exists) {
                return Enumerable.Empty<ConnectionInfo>();
            }

            return connectionDirectory.EnumerateFiles("*.mpack", SearchOption.AllDirectories)
                                      .Select(f => {
                                          try {
                                              return MessagePackSerializer.Deserialize<ConnectionInfo>(
                                                  File.ReadAllBytes(f.FullName),
                                                  GlobalArchiveOption.MessagePackSerializerOptions);
                                          }
                                          catch {
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

            if (!File.Exists(connectionPath)) {
                return null;
            }

            return MessagePackSerializer.Deserialize<ConnectionInfo>(
                File.ReadAllBytes(connectionPath),
                GlobalArchiveOption.MessagePackSerializerOptions);
        }

        public Stream? GetRawCaptureStream(int connectionId)
        {
            var capturePath = Path.Combine(_captureDirectory, $"{connectionId}.pcapng");

            if (!File.Exists(capturePath)) {
                return null;
            }

            return File.Open(capturePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream? GetRawCaptureKeyStream(int connectionId)
        {
            var capturePath = Path.Combine(_captureDirectory, $"{connectionId}.nsskeylog");

            if (!File.Exists(capturePath)) {
                return null;
            }

            return File.Open(capturePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream? GetRequestBody(int exchangeId)
        {
            var requestBodyPath = DirectoryArchiveHelper.GetContentRequestPath(BaseDirectory, exchangeId);

            if (!File.Exists(requestBodyPath)) {
                return null;
            }

            return File.Open(requestBodyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        
        public long GetRequestBodyLength(int exchangeId)
        {
            var cacheKey = $"{nameof(GetRequestBodyLength)}_{exchangeId}";

            if (_lengthCaching.TryGetValue(cacheKey, out var length))
                return length; 

            var requestBodyPath = DirectoryArchiveHelper.GetContentRequestPath(BaseDirectory, exchangeId);
            var fileInfo = new FileInfo(requestBodyPath);

            if (!fileInfo.Exists) {
                return -1;
            }

            if (!EnableFileBodyInfoCaching) {
                return fileInfo.Length;
            }

            return _lengthCaching[cacheKey] = fileInfo.Length;
        }

        public long GetResponseBodyLength(int exchangeId)
        {
            var cacheKey = $"{nameof(GetResponseBodyLength)}_{exchangeId}";

            if (EnableFileBodyInfoCaching && _lengthCaching.TryGetValue(cacheKey, out var length))
                return length;

            var responseBodyPath = DirectoryArchiveHelper.GetContentResponsePath(BaseDirectory, exchangeId);

            try {
                var fileInfo = new FileInfo(responseBodyPath);

                if (!fileInfo.Exists) {
                    return 0;
                }

                if (!EnableFileBodyInfoCaching)
                {
                    return fileInfo.Length;
                }

                return _lengthCaching[cacheKey] = fileInfo.Length;
            }
            catch (IOException)
            {
                // in  cases, the file may be locked by another process
                return 0;
            }
        }

        public Stream? GetRequestWebsocketContent(int exchangeId, int messageId)
        {
            var requestBodyPath =
                DirectoryArchiveHelper.GetWebsocketContentRequestPath(BaseDirectory, exchangeId, messageId);

            if (!File.Exists(requestBodyPath)) {
                return null;
            }

            return File.Open(requestBodyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream? GetResponseWebsocketContent(int exchangeId, int messageId)
        {
            var responseBodyPath =
                DirectoryArchiveHelper.GetWebsocketContentResponsePath(BaseDirectory, exchangeId, messageId);

            if (!File.Exists(responseBodyPath)) {
                return null;
            }

            return File.Open(responseBodyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public bool HasRequestBody(int exchangeId)
        {
            var fileInfo = new FileInfo(DirectoryArchiveHelper.GetContentRequestPath(BaseDirectory, exchangeId));

            return fileInfo.Exists && fileInfo.Length > 0;
        }

        public Stream? GetResponseBody(int exchangeId)
        {
            try {
                var requestContentPath = DirectoryArchiveHelper.GetContentResponsePath(BaseDirectory, exchangeId);

                if (!File.Exists(requestContentPath)) {
                    return null;
                }

                return File.Open(requestContentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (IOException) {
                // in  cases, the file may be locked by another process
                return null;
            }
        }

        public Stream? GetDecodedRequestBody(int exchangeId)
        {
            var exchangeInfo = ReadExchange(exchangeId);

            if (exchangeInfo == null)
                throw new InvalidOperationException($"Exchange {exchangeId} not found on this archive");

            var originalStream = GetRequestBody(exchangeInfo.Id);

            if (originalStream == null)
                return null;

            return exchangeInfo.GetDecodedRequestBodyStream(originalStream, out _);
        }

        public Stream? GetDecodedResponseBody(int exchangeId)
        {
            var exchangeInfo = ReadExchange(exchangeId);

            if (exchangeInfo == null)
                throw new InvalidOperationException($"Exchange {exchangeId} not found on this archive"); 

            var originalStream = GetResponseBody(exchangeInfo.Id);

            if (originalStream == null)
                return null;

            return exchangeInfo.GetDecodedResponseBodyStream(originalStream, out _, true);
        }

        public bool HasResponseBody(int exchangeId)
        {
            try {
                var fileInfo = new FileInfo(DirectoryArchiveHelper.GetContentResponsePath(BaseDirectory, exchangeId));

                return fileInfo.Exists && fileInfo.Length > 0;
            }
            catch (IOException)
            {
                // in  cases, the file may be locked by another process
                return false;
            }
        }

        public bool HasCapture(int connectionId)
        {
            var fileInfo = new FileInfo(DirectoryArchiveHelper.GetCapturePath(BaseDirectory, connectionId));

            return fileInfo.Exists && fileInfo.Length > 0;
        }

        public IEnumerable<ArchiveAsset> GetAssetsByExchange(int exchangeId)
        {
            var fileInfos = new List<FileInfo>();

            var exchangePath = DirectoryArchiveHelper.GetExchangePath(BaseDirectory, exchangeId);

            if (!File.Exists(exchangePath)) {
                yield break; 
            }

            fileInfos.Add(new FileInfo(exchangePath));

            var contentDirectory = DirectoryArchiveHelper.GetContentDirectory(BaseDirectory);

            foreach (var fileInfo in new DirectoryInfo(contentDirectory)
                         .EnumerateFiles($"*.data").Where(e =>
                e.Name.StartsWith($"res-{exchangeId}.")
                || e.Name.StartsWith($"res-{exchangeId}-")
                || e.Name.StartsWith($"req-{exchangeId}.")
                || e.Name.StartsWith($"req-{exchangeId}-")
                || e.Name.StartsWith($"ex-{exchangeId}.")))
            {
                fileInfos.Add(fileInfo);
            }

            foreach (var fileInfo in fileInfos) {
                yield return new ArchiveAsset(fileInfo.GetRelativePath(BaseDirectory), 
                    fileInfo.Length, fileInfo.FullName, () => fileInfo.OpenRead());
            }
        }

        public IEnumerable<ArchiveAsset> GetAssetsByConnection(int connectionId)
        {
            var fileInfos = new List<FileInfo>();

            var connectionPath = DirectoryArchiveHelper.GetConnectionPath(BaseDirectory, connectionId);

            if (!File.Exists(connectionPath)) {
                yield break;
            }

            fileInfos.Add(new FileInfo(connectionPath));

            var connectionDirectory = DirectoryArchiveHelper.GetCaptureDirectory(BaseDirectory);

            foreach (var fileInfo in new DirectoryInfo(connectionDirectory)
                                     .EnumerateFiles().Where(e =>
                                         e.Name.Equals($"{connectionId}.nsskeylog")
                                         || e.Name.Equals($"{connectionId}.pcapng"))) {
                fileInfos.Add(fileInfo);
            }

            foreach (var fileInfo in fileInfos) {
                yield return new ArchiveAsset(fileInfo.GetRelativePath(BaseDirectory), 
                                       fileInfo.Length, fileInfo.FullName, () => fileInfo.OpenRead());
            }
        }

        public void Dispose()
        {
        }

        public string? GetRawCaptureFile(int connectionId)
        {
            var capturePath = Path.Combine(_captureDirectory, $"{connectionId}.pcapng");

            if (!File.Exists(capturePath)) {
                return null;
            }

            return capturePath;
        }
    }


    internal static class FileInfoExtensions
    {
        public static string GetRelativePath(this FileInfo fileInfo, DirectoryInfo parentDirectory)
        {
            if (!fileInfo.FullName.StartsWith(parentDirectory.FullName))
            {
                throw new System.ArgumentException("The parent directory must be a parent of the file.");
            }

            return fileInfo.FullName.Substring(parentDirectory.FullName.Length + 1)
                           .Replace("\\", "/");
        }

        public static string GetRelativePath(this FileInfo fileInfo, string parentDirectory)
        {
            return GetRelativePath(fileInfo, new DirectoryInfo(parentDirectory));
        }
    }
}
