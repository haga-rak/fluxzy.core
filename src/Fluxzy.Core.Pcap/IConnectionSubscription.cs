// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Core.Pcap
{
    public interface IConnectionSubscription : IAsyncDisposable
    {
        long Key { get; }
    }
}
