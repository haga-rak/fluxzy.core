// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;

namespace Fluxzy
{
    internal static class DirectoryArchiveHelper
    {
        private static readonly int MaxItemPerDirectory = 100;

        internal static void CreateDirectory(string fullPath)
        {
            var fullDir = new FileInfo(fullPath).Directory;

            if (fullDir != null)
                Directory.CreateDirectory(fullDir.FullName);
        }

        internal static IEnumerable<FileInfo> EnumerateExchangeFileCandidates(string baseDirectory)
        {
            var targetPath = Path.Combine(baseDirectory, "exchanges");
            var directoryInfo = new DirectoryInfo(targetPath);

            return directoryInfo.EnumerateFiles("ex-*.json", SearchOption.AllDirectories);
        }

        internal static IEnumerable<FileInfo> EnumerateConnectionFileCandidates(string baseDirectory)
        {
            var targetPath = Path.Combine(baseDirectory, "connections");
            var directoryInfo = new DirectoryInfo(targetPath);

            return directoryInfo.EnumerateFiles("con-*.json", SearchOption.AllDirectories);
        }

        internal static string GetExchangePath(string baseDirectory, int exchangeId)
        {
            var baseNumber = exchangeId / MaxItemPerDirectory * 100;
            var directoryHint = $"{baseNumber}-{baseNumber + MaxItemPerDirectory}";

            var preDir = Path.Combine(baseDirectory, "exchanges", directoryHint);

            return Path.Combine(preDir, $"ex-{exchangeId}.json");
        }

        internal static string GetExchangePath(string baseDirectory, ExchangeInfo exchangeInfo)
        {
            return GetExchangePath(baseDirectory, exchangeInfo.Id);
        }

        internal static string GetMetaPath(string baseDirectory)
        {
            return Path.Combine(baseDirectory, "meta.json");
        }

        internal static string GetContentRequestPath(string baseDirectory, int exchangeId)
        {
            return Path.Combine(baseDirectory, "contents", $"req-{exchangeId}.data");
        }

        internal static string GetWebsocketContentRequestPath(string baseDirectory, int exchangeId, int messageId)
        {
            return Path.Combine(baseDirectory, "contents", $"req-{exchangeId}-ws-{messageId}.data");
        }

        internal static string GetWebsocketContentResponsePath(string baseDirectory, int exchangeId, int messageId)
        {
            return Path.Combine(baseDirectory, "contents", $"res-{exchangeId}-ws-{messageId}.data");
        }

        internal static string GetContentRequestPath(string baseDirectory, ExchangeInfo exchangeInfo)
        {
            return GetContentRequestPath(baseDirectory, exchangeInfo.Id);
        }

        internal static string GetContentResponsePath(string baseDirectory, ExchangeInfo exchangeInfo)
        {
            return GetContentResponsePath(baseDirectory, exchangeInfo.Id);
        }

        internal static string GetContentResponsePath(string baseDirectory, int exchangeId)
        {
            return Path.Combine(baseDirectory, "contents", $"res-{exchangeId}.data");
        }

        internal static string GetCapturePath(string baseDirectory, int connectionId)
        {
            return Path.Combine(baseDirectory, "captures", $"{connectionId}.pcapng");
        }

        internal static string GetCapturePathNssKey(string baseDirectory, int connectionId)
        {
            return Path.Combine(baseDirectory, "captures", $"{connectionId}.nsskeylog");
        }

        internal static string GetConnectionPath(string baseDirectory, int connectionId)
        {
            var baseNumber = connectionId / MaxItemPerDirectory * 100;
            var directoryHint = $"{baseNumber}-{baseNumber + MaxItemPerDirectory}";

            var preDir = Path.Combine(baseDirectory, "connections", directoryHint);

            return Path.Combine(preDir, $"con-{connectionId}.json");
        }

        internal static string GetConnectionPath(string baseDirectory, ConnectionInfo connectionInfo)
        {
            return GetConnectionPath(baseDirectory, connectionInfo.Id);
        }
    }
}
