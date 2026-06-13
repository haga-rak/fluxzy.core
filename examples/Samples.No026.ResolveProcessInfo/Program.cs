// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Fluxzy.Utils.ProcessTracking;
using Fluxzy.Writers;

namespace Samples.No026.ResolveProcessInfo
{
    internal class Program
    {
        /// <summary>
        /// Shows how to supply process information yourself instead of relying on the built-in OS TCP
        /// table lookup. This is the seam to use when a transparent tunnel such as tun2socks fronts the
        /// proxy over SOCKS5: the socket the proxy accepts belongs to the tunnel, not the real client,
        /// so only your code can map a connection back to the originating process. Implement
        /// IProcessInfoResolver, pass it to the Proxy constructor, and keep process tracking enabled.
        /// The resolver is called once per downstream connection and its result lands on every exchange.
        /// </summary>
        static async Task Main()
        {
            var fluxzySetting = FluxzySetting
                                .CreateDefault(System.Net.IPAddress.Loopback, 44344)
                                .SetEnableProcessTracking(true);

            await using var proxy = new Proxy(fluxzySetting,
                new CertificateProvider(fluxzySetting.CaCertificate, new InMemoryCertificateCache()),
                new DefaultCertificateAuthorityManager(),
                processInfoResolver: new Tun2SocksProcessResolver());

            proxy.Writer.ExchangeUpdated += (_, e) => {
                var processInfo = e.Original.ProcessInfo;

                if (e.UpdateType == ArchiveUpdateType.AfterResponseHeader && processInfo != null)
                    Console.WriteLine($"{e.Original.KnownAuthority} -> pid {processInfo.ProcessId} {processInfo.ProcessPath}");
            };

            var endpoints = proxy.Run();

            using var httpClient = new HttpClient(new HttpClientHandler {
                Proxy = new System.Net.WebProxy($"http://127.0.0.1:{endpoints.First().Port}"),
                UseProxy = true
            });

            using var response = await httpClient.GetAsync("https://www.example.com/");
            Console.WriteLine($"status {(int) response.StatusCode}");
        }
    }

    internal class Tun2SocksProcessResolver : IProcessInfoResolver
    {
        public ValueTask<ProcessInfo?> ResolveAsync(ProcessResolutionContext context, CancellationToken token)
        {
            // A real integration looks this connection up in the tunnel flow table, keyed by the source
            // port (context.RemoteEndPoint.Port) or the requested destination (context.RequestedAuthority),
            // and returns the originating process. Returning null leaves the exchange without process info.
            //
            // Here we just report the current process to keep the sample self contained.
            var processInfo = new ProcessInfo(
                Environment.ProcessId, Environment.ProcessPath, processArguments: null);

            return new ValueTask<ProcessInfo?>(processInfo);
        }
    }
}
