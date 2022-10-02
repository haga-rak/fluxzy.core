// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Formatters
{
    public interface IArchiveReaderProvider
    {
        Task<IArchiveReader?> Get(); 
    }
}