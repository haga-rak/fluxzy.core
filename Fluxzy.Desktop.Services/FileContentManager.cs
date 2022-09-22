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

        private readonly BehaviorSubject<TrunkState> _subject;

        public FileContentOperationManager(FileState fileState)
        {
            State = fileState;
            _subject = new(ReadDirectory(fileState));
            Observable = _subject.AsObservable();
        }
        
        private static TrunkState ReadDirectory(FileState current)
        {
            var exchangeDir = Path.Combine(current.WorkingDirectory, "exchanges");
            var connectionDir = Path.Combine(current.WorkingDirectory, "connections");
            var exchanges = new List<ExchangeContainer>(); 
            var connections = new List<ConnectionContainer>(); 

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
                catch (IOException)
                {
                    // We ignore read errors (engine is probably writing to file )
                    continue;
                }

                if (exchange != null)
                {
                    var container = new ExchangeContainer(exchange);
                    exchanges.Add(container);
                }
            }

            var connectionFileInfos =
                new DirectoryInfo(connectionDir)
                    .EnumerateFiles("*.json", SearchOption.AllDirectories);

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
                    connections.Add(new ConnectionContainer(connection));
            }

            return new TrunkState(exchanges, connections);
            
        }

        public IObservable<TrunkState> Observable { get; }

        public void AddOrUpdate(ExchangeInfo exchangeInfo)
        {
            lock (_subject) // TODO: think of do we really need a lock 
            {
                var current = _subject.Value;

                var exchangeListFinal = current.Exchanges;

                var newContainer = new ExchangeContainer(exchangeInfo);
              
                exchangeListFinal = !current.ExchangesIndexer.TryGetValue(exchangeInfo.Id, out var exchangeIndex) ?
                    // Add 
                    exchangeListFinal.Add(newContainer) 
                    :
                    // Update 
                    exchangeListFinal.SetItem(exchangeIndex , newContainer);

                current = new TrunkState(exchangeListFinal, current.Connections);

                _subject.OnNext(current);
                State.Owner.SetUnsaved(true);
            }
        }

        public void Delete(FileContentDelete deleteOp)
        {
            lock (_subject)
            {
                var current = _subject.Value;
                var exchangeListFinal = current.Exchanges;

                exchangeListFinal = exchangeListFinal.RemoveAll(e => deleteOp.Identifiers.Contains(e.Id));

                current = new TrunkState(exchangeListFinal, current.Connections);

                _subject.OnNext(current);
                State.Owner.SetUnsaved(true);
            }
        }

        public void Clear()
        {
            lock (_subject)
            {
                var current = _subject.Value;

                var exchangeListFinal = current.Exchanges.Clear();
                var connectionListFinal = current.Connections.Clear(); 

                current = new TrunkState(exchangeListFinal, connectionListFinal);

                _subject.OnNext(current);
                State.Owner.SetUnsaved(true);
            }
        }
    }
} 