using System.Text.Json;
using Fluxzy.Clients;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class TrunkManager
    {
        private Guid? _currentIdentifier;
        private List<ExchangeInfo> _currentExchanges = new(); 
        private List<ConnectionInfo> _currentConnectionInfos = new();

        private async Task ReadDirectory(FileState current)
        {
            var exchangeDir = Path.Combine(current.WorkingDirectory, "exchanges");
            var connectionDir = Path.Combine(current.WorkingDirectory, "connections");

            Directory.CreateDirectory(exchangeDir);
            Directory.CreateDirectory(connectionDir);

            var exchangeFileInfos =
                new DirectoryInfo(exchangeDir)
                    .EnumerateFiles("*.json", SearchOption.AllDirectories);

            var tempList = new List<ExchangeInfo>(); 
            var tempListConnection = new List<ConnectionInfo>(); 

            foreach (var fileInfo in exchangeFileInfos)
            {
                ExchangeInfo? exchange = null;
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
                    tempList.Add(exchange);
            }
            
            var connectionFileInfos =
                    new DirectoryInfo(connectionDir)
                    .EnumerateFiles("*.json", SearchOption.AllDirectories)
                    .OrderBy(o => o.Name);

            foreach (var fileInfo in connectionFileInfos)
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
                    tempListConnection.Add(connection);
            }

            _currentIdentifier = current.Identifier; 
            _currentExchanges = tempList.OrderBy(r => r.Id).ToList();
            _currentConnectionInfos = tempListConnection;
        }

        public async Task<int> ExchangeCount(FileState current)
        {
            if (_currentIdentifier != current.Identifier)
                await ReadDirectory(current);

            return _currentExchanges.Count;
        }

        public async Task<List<ExchangeInfo>> ReadExchanges(FileState current, int start, int count)
        {
            if (_currentIdentifier != current.Identifier)
                await ReadDirectory(current);

            return _currentExchanges.Skip(start).Take(count).ToList();
        }

        public async Task<List<ConnectionInfo>> ReadConnections(FileState current)
        {
            if (_currentIdentifier != current.Identifier)
                await ReadDirectory(current);

            return _currentConnectionInfos;
        }

        public async Task<ExchangeState> ReadState(FileState current, ExchangeBrowsingState browsingState)
        {
            var totalCount = await ExchangeCount(current);

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
            
            var exchanges = (await ReadExchanges(current, startIndex, endIndex)).OrderBy(r => r.Id).ToList();

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