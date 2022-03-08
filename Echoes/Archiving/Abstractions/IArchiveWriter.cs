using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
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


    public class DirectoryArchiveWriter : IArchiveWriter
    {
        private readonly string _baseDirectory;
        private readonly string _contentDirectory;
        private readonly List<Exchange> _entries = new();
        private readonly string _archivePath;
        private readonly ExchangeArchive _archive;

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }; 

        public DirectoryArchiveWriter(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            _contentDirectory  = Path.Combine(baseDirectory, "contents");

            Directory.CreateDirectory(_contentDirectory);

            _archivePath = Path.Combine(baseDirectory, "archives.json");
            _archive = new ExchangeArchive(); 
        }

        private async Task WriteToFile(CancellationToken token)
        {
            await using var fileStream = File.Create(_archivePath);
            await JsonSerializer.SerializeAsync(fileStream,
                _archive, JsonSerializerOptions, token);
        }

        public async Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
            _archive.Exchanges[exchangeInfo.Id] = exchangeInfo; 
            await WriteToFile(cancellationToken);
        }

        public async Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            _archive.Connections[connectionInfo.Id] = connectionInfo;
            await WriteToFile(cancellationToken);
        }

        public Stream CreateRequestBodyStream(int exchangeId)
        {
            var path = Path.Combine(_contentDirectory, $"req-{exchangeId}.data");
            return File.Create(path);
        }

        public Stream CreateResponseBodyStream(int exchangeId)
        {
            var path = Path.Combine(_contentDirectory, $"res-{exchangeId}.data");
            return File.Create(path);
        }
    }


    public class ConnectionInfo
    {
        public int Id { get; set; }
    }
}
