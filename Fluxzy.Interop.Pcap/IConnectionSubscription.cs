// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Interop.Pcap
{
    public interface IConnectionSubscription : IAsyncDisposable
    {
        long Key { get; }
    }
}
