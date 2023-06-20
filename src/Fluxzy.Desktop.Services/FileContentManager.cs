// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Readers;
using Fluxzy.Writers;
using MessagePack;

namespace Fluxzy.Desktop.Services
{
    public class FileContentOperationManager : IFileContentOperationManager
    {
        private readonly BehaviorSubject<TrunkState> _subject;

        public FileContentOperationManager(FileState fileState)
        {
            State = fileState;
            _subject = new BehaviorSubject<TrunkState>(ReadDirectory(fileState));
            Observable = _subject.AsObservable();
        }

        public FileState State { get; }

        public IObservable<TrunkState> Observable { get; }

        public void AddOrUpdate(ExchangeInfo exchangeInfo)
        {
            lock (_subject) // TODO: think of do we really need a lock 
            {
                var current = _subject.Value;

                var exchangeListFinal = current.Exchanges;

                var newContainer = new ExchangeContainer(exchangeInfo);

                if (!current.ExchangesIndexer.TryGetValue(exchangeInfo.Id, out var exchangeIndex))
                    exchangeListFinal.Add(newContainer);
                else
                    exchangeListFinal[exchangeIndex] = newContainer;

                current = new TrunkState(exchangeListFinal, current.Connections, current.ErrorCount);

                _subject.OnNext(current);
                State.Owner.SetUnsaved(true);
            }
        }

        public void Delete(FileContentDelete deleteOp)
        {
            lock (_subject) {
                var current = _subject.Value;
                var exchangeListFinal = current.Exchanges;

                exchangeListFinal.RemoveAll(e => deleteOp.Identifiers.Contains(e.Id));

                current = new TrunkState(exchangeListFinal, current.Connections, current.ErrorCount);

                _subject.OnNext(current);
                State.Owner.SetUnsaved(true);
            }
        }

        public void ClearErrors(ForwardMessageManager forwardMessageManager, RealtimeArchiveWriter realtimeArchiveWriter)
        {
            realtimeArchiveWriter.ClearErrors();

            UpdateErrorCount(0);

            forwardMessageManager.Send(new DownstreamCountUpdate(0));
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
                    .EnumerateFiles("*.mpack", SearchOption.AllDirectories);

            foreach (var fileInfo in exchangeFileInfos) {
                ExchangeInfo? exchange;

                try {
                    using var stream = fileInfo.OpenRead();

                    exchange = MessagePackSerializer.Deserialize<ExchangeInfo>(stream,
                        GlobalArchiveOption.MessagePackSerializerOptions); 
                }
                catch (IOException) {
                    // We ignore read errors (engine is probably writing to file )
                    continue;
                }

                if (exchange != null) {
                    var container = new ExchangeContainer(exchange);
                    exchanges.Add(container);
                }
            }

            var connectionFileInfos =
                new DirectoryInfo(connectionDir)
                    .EnumerateFiles("*.mpack", SearchOption.AllDirectories);

            foreach (var fileInfo in connectionFileInfos) {
                ConnectionInfo? connection = null;

                try {
                    using var stream = fileInfo.OpenRead();

                    connection =
                     MessagePackSerializer.Deserialize<ConnectionInfo>(
                        stream,
                        GlobalArchiveOption.MessagePackSerializerOptions);
                }
                catch {
                    // We ignore read errors (engine is writing to file )
                    continue;
                }

                connections.Add(new ConnectionContainer(connection));
            }

            exchanges.Sort(ExchangeContainerSorter.IdSorter);

            var directoryReader = new DirectoryArchiveReader(current.WorkingDirectory);

            var errorCount = directoryReader.ReaderAllDownstreamErrors().Count; 
            return new TrunkState(exchanges, connections, errorCount);
        }

        public void AddOrUpdate(ConnectionInfo connectionInfo)
        {
            lock (_subject) // TODO: think of do we really need a lock 
            {
                var current = _subject.Value;

                var connectionListFinal = current.Connections;

                var newContainer = new ConnectionContainer(connectionInfo);

                if (!current.ConnectionsIndexer.TryGetValue(connectionInfo.Id, out var connectionIndex))
                    connectionListFinal.Add(newContainer);
                else
                    connectionListFinal[connectionIndex] = newContainer;

                current = new TrunkState(current.Exchanges, connectionListFinal, current.ErrorCount);

                _subject.OnNext(current);
                State.Owner.SetUnsaved(true);
            }
        }

        public void Clear()
        {
            lock (_subject) {
                var current = _subject.Value;

                var exchangeListFinal = current.Exchanges;
                var connectionListFinal = current.Connections;

                exchangeListFinal.Clear();
                connectionListFinal.Clear();

                current = new TrunkState(exchangeListFinal, connectionListFinal, current.ErrorCount);

                _subject.OnNext(current);
                State.Owner.SetUnsaved(true);
            }
        }

        public void UpdateErrorCount(int errorCount)
        {
            lock (_subject) // TODO: think of do we really need a lock 
            {
                var current = _subject.Value;
                
                current = new TrunkState(current.Exchanges, current.Connections, errorCount);

                _subject.OnNext(current);
                State.Owner.SetUnsaved(true);
            }
        }
    }
}
