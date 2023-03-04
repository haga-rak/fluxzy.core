// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Formatters
{
    public interface IArchiveReaderProvider
    {
        Task<IArchiveReader?> Get();
    }
}
