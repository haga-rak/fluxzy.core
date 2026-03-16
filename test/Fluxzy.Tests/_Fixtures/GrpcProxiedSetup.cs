using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Grpc.Net.Client;

namespace Fluxzy.Tests._Fixtures;

public class GrpcProxiedSetup : IAsyncDisposable
{
    private readonly GrpcInProcessHost _host;
    private readonly Proxy _proxy;

    public GrpcChannel Channel { get; }

    public Proxy Proxy => _proxy;

    public FluxzySetting Setting { get; }

    private GrpcProxiedSetup(
        GrpcInProcessHost host, Proxy proxy, GrpcChannel channel, FluxzySetting setting)
    {
        _host = host;
        _proxy = proxy;
        Channel = channel;
        Setting = setting;
    }

    public static async Task<GrpcProxiedSetup> Create(
        Action<FluxzySetting>? configureSetting = null)
    {
        var host = await GrpcInProcessHost.Create();

        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetServeH2(true);
        setting.AddAlterationRules(
            new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

        configureSetting?.Invoke(setting);

        var proxy = new Proxy(setting);
        var proxyEndPoint = proxy.Run().First();

        // Normalize endpoint
        if (proxyEndPoint.Address.Equals(IPAddress.Any))
            proxyEndPoint = new IPEndPoint(IPAddress.Loopback, proxyEndPoint.Port);
        else if (proxyEndPoint.Address.Equals(IPAddress.IPv6Any))
            proxyEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, proxyEndPoint.Port);

        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(proxyEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(proxyEndPoint, cancellationToken);

                var stream = new NetworkStream(socket, ownsSocket: true);
                await Socks5ClientFactory.PerformSocks5HandshakeAsync(
                    stream, context.DnsEndPoint, cancellationToken);

                return stream;
            },
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
                ApplicationProtocols = new List<SslApplicationProtocol>
                {
                    SslApplicationProtocol.Http2
                }
            }
        };

        var channel = GrpcChannel.ForAddress(host.BaseUrl, new GrpcChannelOptions
        {
            HttpHandler = handler
        });

        return new GrpcProxiedSetup(host, proxy, channel, setting);
    }

    public async ValueTask DisposeAsync()
    {
        Channel.Dispose();
        await _proxy.DisposeAsync();
        await _host.DisposeAsync();
    }
}
