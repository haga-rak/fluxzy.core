using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Archiving.Abstractions
{
    internal interface IArchiveWriter
    {
        Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken); 

        Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken);

        Task RegisterRequestBody(int exchangeId, Stream stream, CancellationToken cancellationToken); 

        Task RegisterResponseBody(int exchangeId, Stream stream, CancellationToken cancellationToken); 
    }


    public class DirectoryArchiveWriter : IArchiveWriter
    {
        private readonly string _baseDirectory;
        private readonly string _contentDirectory;
        private readonly List<Exchange> _entries = new(); 

        public DirectoryArchiveWriter(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            _contentDirectory  = Path.Combine(baseDirectory, "content");

            Directory.CreateDirectory(_contentDirectory);
        }

        public Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RegisterRequestBody(int exchangeId, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RegisterResponseBody(int exchangeId, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }


    public class ConnectionInfo
    {

    }
}
