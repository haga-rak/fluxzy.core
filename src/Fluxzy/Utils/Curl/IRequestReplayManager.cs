// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Readers;

namespace Fluxzy.Utils.Curl
{
    public interface IRequestReplayManager
    {
        Task<bool> Replay(IArchiveReader archiveReader, ExchangeInfo exchangeInfo, bool runInLiveEdit = false);
    }
}
