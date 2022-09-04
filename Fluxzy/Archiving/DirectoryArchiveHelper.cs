using Fluxzy.Clients;
using System.IO;

namespace Fluxzy
{
    internal static class DirectoryArchiveHelper
    {
        private static readonly int MaxItemPerDirectory = 100;

        internal static string GetExchangePath(string baseDirectory, ExchangeInfo exchangeInfo)
        {
            var baseNumber = (exchangeInfo.Id / MaxItemPerDirectory) * 100; 
            var directoryHint = $"{baseNumber}-{(baseNumber + MaxItemPerDirectory)}";

            var preDir = Path.Combine(baseDirectory, "exchanges", directoryHint);

            Directory.CreateDirectory(preDir);

            return Path.Combine(preDir, $"ex-{exchangeInfo.Id}.json");
        }
        internal static string GetContentRequestPath(string baseDirectory, ExchangeInfo exchangeInfo)
        {
            return Path.Combine(baseDirectory, "contents", $"req-{exchangeInfo.Id}.data");
        }

        internal static string GetContentResponsePath(string baseDirectory, ExchangeInfo exchangeInfo)
        {
            return Path.Combine(baseDirectory, "contents", $"res-{exchangeInfo.Id}.data");
        }

        internal static string GetConnectionPath(string baseDirectory, ConnectionInfo connectionInfo)
        {
            var baseNumber = (connectionInfo.Id / MaxItemPerDirectory) * 100; 
            var directoryHint = $"{(baseNumber)}-{(baseNumber + MaxItemPerDirectory)}";

            var preDir = Path.Combine(baseDirectory, "connections", directoryHint);

            Directory.CreateDirectory(preDir);

            return Path.Combine(preDir, $"con-{connectionInfo.Id}.json");
        }
    }
}