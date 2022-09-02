using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public interface IFileContentOperationManager
    {
        void AddOrUpdate(ExchangeInfo exchangeInfo);

        void Delete(FileContentDelete deleteOp);
    }

    public class FileContentOperationManager : IFileContentOperationManager
    {
        public FileState State { get; }

        private BehaviorSubject<TrunkState> _subject;

        public FileContentOperationManager(FileState fileState)
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

        public void AddOrUpdate(ExchangeInfo exchangeInfo)
        {
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

                _subject.OnNext(current);
            }
        }

        public void Delete(FileContentDelete deleteOp)
        {
            var current = _subject.Value;

            lock (current)
            {
                var removableIndexes = current.Exchanges
                    .Select((e, index) => new
                    {
                        Exchange = e, 
                        Index = index
                    })
                    .Where(c =>
                        deleteOp.Identifiers.Contains(c.Exchange.Id))
                    .Select(c => c.Index)
                    .ToList();
                
                foreach (var removableIndex in removableIndexes)
                {
                    current.Exchanges.RemoveAt(removableIndex);
                }

                foreach (var id in deleteOp.Identifiers)
                {
                    current.ExchangeIndex.Remove(id);
                }

                _subject.OnNext(current);
            }
        }
    }
} 