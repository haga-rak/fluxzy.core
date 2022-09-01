using System.Reactive.Linq;
using System.Text.Json;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class TrunkManager : IObservableProvider<TrunkState>
    {
        private List<ExchangeInfo> _currentExchanges = new(); 
        private List<ConnectionInfo> _currentConnectionInfos = new();

        public TrunkManager(IObservable<FileState> fileState)
        {
            Observable =
                fileState
                    .Select(fs => System.Reactive.Linq.Observable.Create<TrunkState>(
                    async (next, state) =>
                    {
                        await ReadDirectory(fs);

                        var result = new TrunkState(_currentExchanges, _currentConnectionInfos);

                        next.OnNext(result);
                        next.OnCompleted();
                    }))
                    .Switch();

            Observable.Do(ts => Current = ts).Subscribe();
        }

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

            _currentExchanges = tempList.OrderBy(r => r.Id).ToList();
            _currentConnectionInfos = tempListConnection;
        }
        
        public TrunkState? Current { get; private set; }

        public IObservable<TrunkState> Observable { get; }
    }
}