// Copyright © 2023 Haga RAKOTOHARIVELO

using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Utils.Curl
{
    public interface IRequestReplayManager
    {
        Task<bool> Replay(IArchiveReader archiveReader, ExchangeInfo exchangeInfo);
    }
}
