// Copyright © 2022 Haga RAKOTOHARIVELO

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
