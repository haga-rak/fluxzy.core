using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Echoes.Archiving.Abstractions
{
    internal interface IArchiveWriter
    {
        Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken); 

        Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken);

        Stream CreateRequestBodyStream(int exchangeId);

        Stream CreateResponseBodyStream(int exchangeId);
    }


    public abstract class RealtimeArchiveWriter : IArchiveWriter
    {

        public virtual async Task Update(Connection connection, CancellationToken cancellationToken)
        {
            ConnectionInfo connectionInfo = new ConnectionInfo(connection);

            await Update(connectionInfo, cancellationToken); 

        }
        public virtual async Task Update(Exchange exchange, CancellationToken cancellationToken)
        {
            await Update(new ExchangeInfo(exchange), cancellationToken); 
        }

        public abstract Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken);

        public abstract Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken);

        public abstract Stream CreateRequestBodyStream(int exchangeId);

        public abstract Stream CreateResponseBodyStream(int exchangeId);
    }

    public class DirectoryArchiveWriter : RealtimeArchiveWriter
    {
        private static readonly int MaxItemPerDirectory = 100; 

        private readonly string _baseDirectory;
        private readonly string _contentDirectory;

        public DirectoryArchiveWriter(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            _contentDirectory  = Path.Combine(baseDirectory, "contents");

            Directory.CreateDirectory(_contentDirectory);
        }

        private string GetExchangePath(ExchangeInfo exchangeInfo)
        {
            var baseNumber = (exchangeInfo.Id / MaxItemPerDirectory) * 100; 
            var directoryHint = $"{baseNumber}-{(baseNumber + MaxItemPerDirectory)}";

            var preDir = Path.Combine(_baseDirectory, "exchanges", directoryHint);

            Directory.CreateDirectory(preDir);

            return Path.Combine(preDir, $"ex-{exchangeInfo.Id}.json");
        }

        private string GetConnectionPath(ConnectionInfo connectionInfo)
        {
            var baseNumber = (connectionInfo.Id / MaxItemPerDirectory) * 100; 
            var directoryHint = $"{(baseNumber)}-{(baseNumber + MaxItemPerDirectory)}";

            var preDir = Path.Combine(_baseDirectory, "connections", directoryHint);

            Directory.CreateDirectory(preDir);

            return Path.Combine(preDir, $"con-{connectionInfo.Id}.json");
        }


        public override async Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
            var exchangePath = GetExchangePath(exchangeInfo);
            await using var fileStream = File.Create(exchangePath);
            await JsonSerializer.SerializeAsync(fileStream, exchangeInfo, GlobalArchiveOption.JsonSerializerOptions, cancellationToken);
        }

        public override async Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            var connectionPath = GetConnectionPath(connectionInfo);
            await using var fileStream = File.Create(connectionPath);
            await JsonSerializer.SerializeAsync(fileStream, connectionInfo, GlobalArchiveOption.JsonSerializerOptions, cancellationToken);
        }

        public override Stream CreateRequestBodyStream(int exchangeId)
        {
            var path = Path.Combine(_contentDirectory, $"req-{exchangeId}.data");
            return File.Create(path);
        }

        public override Stream CreateResponseBodyStream(int exchangeId)
        {
            var path = Path.Combine(_contentDirectory, $"res-{exchangeId}.data");
            return File.Create(path);
        }
    }
}
