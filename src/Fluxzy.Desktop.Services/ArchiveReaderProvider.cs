// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Formatters;
using Fluxzy.Readers;

namespace Fluxzy.Desktop.Services
{
    public class ArchiveReaderProvider : IArchiveReaderProvider
    {
        private readonly IObservable<FileState> _fileStateProvider;

        public ArchiveReaderProvider(IObservable<FileState> fileStateProvider)
        {
            _fileStateProvider = fileStateProvider;
        }

        public async Task<IArchiveReader?> Get()
        {
            var fileState = await _fileStateProvider.FirstOrDefaultAsync();

            if (fileState is null)
                return null;

            return new DirectoryArchiveReader(fileState.WorkingDirectory);
        }
    }
}
