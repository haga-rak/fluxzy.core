using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class FileContentManager
    {
        public FileState State { get; }

        private BehaviorSubject<TrunkState> _subject;

        public FileContentManager(FileState fileState)
        {
            State = fileState;
            _subject = new(ReadDirectory(fileState));
            Observable = _subject.AsObservable();
        }
        
        private static TrunkState ReadDirectory(FileState current)
        {
            var result = new TrunkState(new(), new());

            var exchangeDir = Path.Combine(current.WorkingDirectory, "exchanges");
            var connectionDir = Path.Combine(current.WorkingDirectory, "connections");

            Directory.CreateDirectory(exchangeDir);
            Directory.CreateDirectory(connectionDir);

            var exchangeFileInfos =
                new DirectoryInfo(exchangeDir)
                    .EnumerateFiles("*.json", SearchOption.AllDirectories);

            foreach (var fileInfo in exchangeFileInfos)
            {
                ExchangeInfo? exchange = null;
                try
                {
                    exchange = JsonSerializer.Deserialize<ExchangeInfo>(fileInfo.OpenRead(),
                        GlobalArchiveOption.JsonSerializerOptions);
                }
                catch
                {
                    // We ignore read errors (engine is probably writing to file )
                    continue;
                }

                if (exchange != null)
                {
                    var container = new ExchangeContainer(exchange);
                    result.ExchangeIndex[container.Id] = container;
                    result.Exchanges.Add(container);
                }
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
                    connection = JsonSerializer.Deserialize<ConnectionInfo>(fileInfo.OpenRead(),
                        GlobalArchiveOption.JsonSerializerOptions);
                }
                catch
                {
                    // We ignore read errors (engine is writing to file )
                    continue;
                }

                if (connection != null)
                    result.Connections.Add(new ConnectionContainer(connection));
            }

            return result ;
        }

        public IObservable<TrunkState> Observable { get; }

        public void Update(ExchangeInfo exchangeInfo, FileState fileState)
        {
            if (fileState.Identifier != State.Identifier)
                return; // No for current 

            var current = _subject.Value;

            lock (current)
            {
                if (!current.ExchangeIndex.TryGetValue(exchangeInfo.Id, out var container))
                {
                    container = new ExchangeContainer(exchangeInfo);
                    current.ExchangeIndex[exchangeInfo.Id] = container;
                    current.Exchanges.Add(container);
                }

                container.ExchangeInfo = exchangeInfo;
            }

            _subject.OnNext(current);

        }
    }
}