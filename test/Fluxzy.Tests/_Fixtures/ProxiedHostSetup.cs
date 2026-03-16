using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Microsoft.AspNetCore.Builder;

namespace Fluxzy.Tests._Fixtures
{
    /// <summary>
    ///     Bundles an in-process Kestrel HTTPS server with a Fluxzy proxy in front of it.
    ///     Produces an HttpClient that routes through Fluxzy via SOCKS5.
    /// </summary>
    public class ProxiedHostSetup : IAsyncDisposable
    {
        private readonly InProcessHost _host;
        private readonly Proxy _proxy;

        public HttpClient Client { get; }

        public Proxy Proxy => _proxy;

        public FluxzySetting Setting { get; }

        public string BaseUrl => _host.BaseUrl;

        private ProxiedHostSetup(
            InProcessHost host, Proxy proxy, HttpClient client, FluxzySetting setting)
        {
            _host = host;
            _proxy = proxy;
            Client = client;
            Setting = setting;
        }

        public static async Task<ProxiedHostSetup> Create(
            Action<FluxzySetting>? configureSetting = null,
            Action<WebApplication>? configureRoutes = null,
            Version? httpVersion = null)
        {
            var host = await InProcessHost.Create(configureRoutes);

            var setting = FluxzySetting.CreateLocalRandomPort();

            // Always skip remote cert validation so Fluxzy accepts the self-signed upstream cert
            setting.AddAlterationRules(
                new SkipRemoteCertificateValidationAction(),
                AnyFilter.Default
            );

            configureSetting?.Invoke(setting);

            var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = endPoints.First();

            var client = Socks5ClientFactory.Create(proxyEndPoint, httpVersion: httpVersion);
            client.BaseAddress = new Uri(host.BaseUrl);

            return new ProxiedHostSetup(host, proxy, client, setting);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _proxy.DisposeAsync();
            await _host.DisposeAsync();
        }
    }
}
