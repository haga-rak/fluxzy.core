using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fluxzy
{
    public interface IDirectoryPackager
    {
        bool ShouldApplyTo(string fileName); 

        Task Pack(string directory, Stream outputStream);

        Task Pack(string directory, Stream outputStream, IEnumerable<ExchangeInfo> exchangeInfos,
            IEnumerable<ConnectionInfo> connectionInfos);

        internal static async Task<Dictionary<int, ConnectionInfo>> ReadConnectionInfos(DirectoryInfo directoryInfo)
        {
            Dictionary<int, ConnectionInfo> connectionInfos = new Dictionary<int, ConnectionInfo>();

            var connectionInfoFiles = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "connections"))
                .EnumerateFiles("*.json", SearchOption.AllDirectories).ToList();


            foreach (var connectionInfofile in connectionInfoFiles)
            {
                if (!connectionInfofile.Name.StartsWith("con-") || connectionInfofile.Length == 0)
                    continue;

                if (
                    !int.TryParse(connectionInfofile.Name.Replace("con-", string.Empty).Replace(".json", string.Empty),
                        out var connectionId))
                {
                    continue;
                }

                using var stream = connectionInfofile.Open(FileMode.Open);

                var connectionInfo = (await JsonSerializer.DeserializeAsync<ConnectionInfo>(
                    stream, GlobalArchiveOption.DefaultSerializerOptions))!;

                connectionInfos[connectionId] = connectionInfo;
            }

            return connectionInfos;
        }

        internal static async IAsyncEnumerable<ExchangeInfo> ReadExchanges(DirectoryInfo directoryInfo)
        {
            var requestFiles =
                new DirectoryInfo(Path.Combine(directoryInfo.FullName, "exchanges"))
                    .EnumerateFiles("*.json", SearchOption.AllDirectories).ToList();

            foreach (var requestFile in requestFiles)
            {
                if (!requestFile.Name.StartsWith("ex-") || requestFile.Length == 0)
                    continue;

                if (
                    !int.TryParse(requestFile
                            .Name.Replace("ex-", string.Empty).Replace(".json", string.Empty),
                        out var exchangeId))
                {
                    continue;
                }

                using var stream = requestFile.Open(FileMode.Open);

                var exchangeInfo = await JsonSerializer.DeserializeAsync<ExchangeInfo>(
                    stream, GlobalArchiveOption.DefaultSerializerOptions);

                if (exchangeInfo == null)
                    continue;

                yield return exchangeInfo;
            }
        }
    }
}