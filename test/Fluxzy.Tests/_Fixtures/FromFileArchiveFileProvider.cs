// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Formatters;
using Fluxzy.Readers;

namespace Fluxzy.Tests._Fixtures
{
    internal class FromFileArchiveFileProvider : IArchiveReaderProvider
    {
        public FromFileArchiveFileProvider(string fileName)
        {
            ArchiveReader = new FluxzyArchiveReader(fileName); 
        }

        private FluxzyArchiveReader ArchiveReader { get;  }

        public Task<IArchiveReader?> Get()
        {
            return Task.FromResult<IArchiveReader?>(ArchiveReader);
        }
    }
}
