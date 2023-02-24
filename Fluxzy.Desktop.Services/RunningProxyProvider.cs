// Copyright © 2023 Haga RAKOTOHARIVELO

using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Utils;
using Fluxzy.Utils.Curl;

namespace Fluxzy.Desktop.Services
{
    public class RunningProxyProvider : IRunningProxyProvider
    {
        private readonly IObservable<ProxyState> _proxyStateProvider;

        public RunningProxyProvider(IObservable<ProxyState> proxyStateProvider)
        {
            _proxyStateProvider = proxyStateProvider;
        }

        public async Task<IRunningProxyConfiguration> GetConfiguration()
        {
            var proxyState = await _proxyStateProvider.FirstAsync();
            return new CurlProxyConfiguration("127.0.0.1", proxyState.BoundConnections.First().Port);
        }
    }
}
