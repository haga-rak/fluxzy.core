using System.Text.Json;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class TrunkManager
    {
        private readonly GlobalFileManager _globalFileManager;

        public TrunkManager(GlobalFileManager globalFileManager)
        {
            _globalFileManager = globalFileManager;
        }

        public Task<int> ExchangeCount()
        {
            var current = _globalFileManager.Current;

            if (current == null)
                return Task.FromResult(0);

            var exchangeDir = Path.Combine(current.WorkingDirectory, "exchanges");

            Directory.CreateDirectory(exchangeDir); 

            var count =
                new DirectoryInfo(exchangeDir)
                    .EnumerateFiles("*.json", SearchOption.AllDirectories).Count();

            return Task.FromResult(count);
        }

        public async IAsyncEnumerable<ExchangeInfo> ReadExchanges(int start, int count)
        {
            var current = _globalFileManager.Current;

            if (current == null)
                yield break;

            var exchangeDir = Path.Combine(current.WorkingDirectory, "exchanges");

            Directory.CreateDirectory(exchangeDir);

            var fileInfos =
                new DirectoryInfo(exchangeDir)
                    .EnumerateFiles("*.json", SearchOption.AllDirectories)
                    .OrderBy(o => o.Name)
                    .Skip(start)
                    .Take(count);

            foreach (var fileInfo in fileInfos)
            {
                ExchangeInfo ? exchange = null; 
                try
                {
                    exchange = await JsonSerializer.DeserializeAsync<ExchangeInfo>(fileInfo.OpenRead(),
                        GlobalArchiveOption.JsonSerializerOptions);
                }
                catch
                {
                    // We ignore read errors (engine is probably writing to file )
                    continue; 
                }

                if (exchange != null)
                    yield return exchange;
            }
        }

        public async IAsyncEnumerable<ConnectionInfo> ReadConnections()
        {
            var current = _globalFileManager.Current;

            if (current == null)
                yield break;

            var fileInfos =
                new DirectoryInfo(Path.Combine(current.WorkingDirectory, "connections"))
                    .EnumerateFiles("*.json", SearchOption.AllDirectories)
                    .OrderBy(o => o.Name); 

            foreach (var fileInfo in fileInfos)
            {
                ConnectionInfo? connection = null;
                try
                {
                    connection = await JsonSerializer.DeserializeAsync<ConnectionInfo>(fileInfo.OpenRead(),
                        GlobalArchiveOption.JsonSerializerOptions);
                }
                catch
                {
                    // We ignore read errors (engine is writing to file )
                    continue;
                }

                if (connection != null)
                    yield return connection;
            }
        }

        public async Task<ExchangeState> ReadState(ExchangeBrowsingState browsingState)
        {
            var totalCount = await ExchangeCount();

            int endIndex, startIndex;

            if (browsingState.EndIndex == null)
            {
                endIndex = totalCount;
                startIndex = endIndex - totalCount; 
            }
            else
            {
                if (browsingState.StartIndex == null)
                {
                    endIndex = browsingState.EndIndex.Value;
                    startIndex = endIndex - totalCount;
                }
                else
                {
                    startIndex = browsingState.StartIndex.Value;
                    endIndex = Math.Min(
                        startIndex + totalCount,
                        startIndex + browsingState.Count); 
                }
            }
            
            var exchanges = (await ReadExchanges(startIndex, endIndex).ToListAsync()).OrderBy(r => r.Id).ToList();

            return new ExchangeState()
            {
                StartIndex = startIndex,
                Count = exchanges.Count,
                EndIndex = endIndex,
                Exchanges = exchanges,
                TotalCount = totalCount
            }; 
        }
    }
}