// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Core.Socks5;
using Fluxzy.Rules;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    /// <summary>
    ///     End-to-end coverage of RecoverHostNameFromSni at the ingress-provider level: the authority
    ///     rewrite + IP pinning in Socks5SourceProvider and FromProxyConnectSourceProvider, and the
    ///     resulting guarantee that the pinned IP keeps the upstream connection off the DNS resolver.
    /// </summary>
    public class SniHostRecoveryProviderTests
    {
        private static readonly IPAddress TargetIp = IPAddress.Parse("1.2.3.4");
        private const int TargetPort = 443;

        // #1 - SOCKS5: IP target + hostname SNI -> authority adopts the hostname, IP is pinned.
        [Fact]
        public async Task Socks5_IpTarget_WithHostnameSni_RewritesAuthorityAndPinsIp()
        {
            var (result, _) = await RunSocks5(
                recoverHostNameFromSni: true, target: Ipv4ConnectRequest(TargetIp, TargetPort),
                clientSniTargetHost: "example.com");

            var exchange = AssertResult(result);

            Assert.Equal("example.com", exchange.Authority.HostName);
            Assert.Equal(TargetPort, exchange.Authority.Port);
            Assert.True(exchange.Authority.Secure);
            Assert.Equal(TargetIp, exchange.Context.RemoteHostIp);
        }

        // #1 - SOCKS5: IP target but no usable SNI (client targets the IP itself) -> IP CN is kept.
        [Fact]
        public async Task Socks5_IpTarget_WithoutUsableSni_KeepsIpAuthorityAndDoesNotPin()
        {
            var (result, _) = await RunSocks5(
                recoverHostNameFromSni: true, target: Ipv4ConnectRequest(TargetIp, TargetPort),
                clientSniTargetHost: TargetIp.ToString());

            var exchange = AssertResult(result);

            Assert.Equal(TargetIp.ToString(), exchange.Authority.HostName);
            Assert.Null(exchange.Context.RemoteHostIp);
        }

        // #1 - SOCKS5: hostname target (ATYP=DOMAIN) is never rewritten, even with the flag on and a
        // usable SNI. The recovery is gated to IP authorities only.
        [Fact]
        public async Task Socks5_HostnameTarget_IsNeverRewritten()
        {
            var (result, _) = await RunSocks5(
                recoverHostNameFromSni: true, target: DomainConnectRequest("example.org", TargetPort),
                clientSniTargetHost: "example.com");

            var exchange = AssertResult(result);

            Assert.Equal("example.org", exchange.Authority.HostName);
            Assert.Null(exchange.Context.RemoteHostIp);
        }

        // #1 - transparent CONNECT: IP target + hostname SNI -> authority adopts the hostname, IP pinned.
        [Fact]
        public async Task Connect_IpTarget_WithHostnameSni_RewritesAuthorityAndPinsIp()
        {
            var (result, _) = await RunConnect(
                recoverHostNameFromSni: true, connectTarget: $"{TargetIp}:{TargetPort}",
                clientSniTargetHost: "example.com");

            var exchange = AssertResult(result);

            Assert.Equal("example.com", exchange.Authority.HostName);
            Assert.Equal(TargetPort, exchange.Authority.Port);
            Assert.True(exchange.Authority.Secure);
            Assert.Equal(TargetIp, exchange.Context.RemoteHostIp);
        }

        // #2 - the pin set during recovery keeps the upstream connection on the exact target IP and the
        // DNS resolver is never consulted for the recovered hostname.
        [Fact]
        public async Task Recovery_PinnedIp_KeepsUpstreamOffTheDnsResolver()
        {
            var (result, dns) = await RunSocks5(
                recoverHostNameFromSni: true, target: Ipv4ConnectRequest(TargetIp, TargetPort),
                clientSniTargetHost: "example.com");

            var exchange = AssertResult(result);
            Assert.Equal("example.com", exchange.Authority.HostName);
            Assert.Equal(TargetIp, exchange.Context.RemoteHostIp);

            // The upstream resolution honors the pin: it returns the original target IP, not the
            // (deliberately different) address the resolver would hand back, and never queries it.
            var resolution = await DnsUtility.ComputeDnsUpdateExchange(
                exchange, ITimingProvider.Default, dns, null);

            Assert.Equal(TargetIp, resolution.EndPoint.Address);
            Assert.Equal(TargetPort, resolution.EndPoint.Port);
            Assert.Empty(dns.Queries);
        }

        // #2 (contrast) - without recovery the authority stays the IP and is not pinned, so the resolver
        // IS on the upstream path. This is the reroute risk that pinning removes.
        [Fact]
        public async Task WithoutRecovery_UpstreamGoesThroughTheDnsResolver()
        {
            var (result, dns) = await RunSocks5(
                recoverHostNameFromSni: false, target: Ipv4ConnectRequest(TargetIp, TargetPort),
                clientSniTargetHost: "example.com");

            var exchange = AssertResult(result);
            Assert.Equal(TargetIp.ToString(), exchange.Authority.HostName);
            Assert.Null(exchange.Context.RemoteHostIp);

            var resolution = await DnsUtility.ComputeDnsUpdateExchange(
                exchange, ITimingProvider.Default, dns, null);

            Assert.Equal(dns.Answer, resolution.EndPoint.Address);
            Assert.Contains(TargetIp.ToString(), dns.Queries);
        }

        private static Exchange AssertResult(ExchangeSourceInitResult? result)
        {
            Assert.NotNull(result);
            return result!.ProvisionalExchange;
        }

        private static async Task<(ExchangeSourceInitResult? Result, SpyDnsSolver Dns)> RunSocks5(
            bool recoverHostNameFromSni, byte[] target, string clientSniTargetHost)
        {
            return await RunProvider(recoverHostNameFromSni,
                (updater, idProvider, contextBuilder, dnsSolver) =>
                    new Socks5SourceProvider(updater, idProvider, NoAuthenticationMethod.Instance,
                        contextBuilder, dnsSolver),
                async (clientStream, token) => {
                    await clientStream.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, token);
                    await clientStream.ReadExactlyAsync(new byte[2], token); // method selection
                    await clientStream.WriteAsync(target, token);
                    await clientStream.ReadExactlyAsync(new byte[10], token); // IPv4 reply
                },
                clientSniTargetHost);
        }

        private static async Task<(ExchangeSourceInitResult? Result, SpyDnsSolver Dns)> RunConnect(
            bool recoverHostNameFromSni, string connectTarget, string clientSniTargetHost)
        {
            return await RunProvider(recoverHostNameFromSni,
                (updater, idProvider, contextBuilder, _) =>
                    new FromProxyConnectSourceProvider(updater, idProvider, NoAuthenticationMethod.Instance,
                        contextBuilder),
                async (clientStream, token) => {
                    var connect = Encoding.ASCII.GetBytes(
                        $"CONNECT {connectTarget} HTTP/1.1\r\nHost: {connectTarget}\r\n\r\n");
                    await clientStream.WriteAsync(connect, token);
                    await ReadUntilDoubleCrlf(clientStream, token); // tunnel accept response
                },
                clientSniTargetHost);
        }

        private static async Task<(ExchangeSourceInitResult? Result, SpyDnsSolver Dns)> RunProvider(
            bool recoverHostNameFromSni,
            Func<SecureConnectionUpdater, IIdProvider, IExchangeContextBuilder, IDnsSolver, ExchangeSourceProvider>
                providerFactory,
            Func<Stream, CancellationToken, Task> preTlsClientDriver,
            string clientSniTargetHost)
        {
            var root = Certificate.UseDefault();
            using var certProvider = new CertificateProvider(root, new InMemoryCertificateCache());
            var setting = FluxzySetting.CreateLocalRandomPort();

            var updater = new SecureConnectionUpdater(certProvider, serveH2: false,
                recoverHostNameFromSni: recoverHostNameFromSni);

            var dns = new SpyDnsSolver(IPAddress.Parse("9.9.9.9"));
            var provider = providerFactory(updater, new FromIndexIdProvider(0, 0),
                new TestExchangeContextBuilder(setting), dns);

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var serverTask = Task.Run(async () => {
                using var serverConnection = await listener.AcceptTcpClientAsync(cts.Token);
                using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(1024 * 8);

                return await provider.InitClientConnection(
                    serverConnection.GetStream(), buffer,
                    (IPEndPoint) serverConnection.Client.LocalEndPoint!,
                    (IPEndPoint) serverConnection.Client.RemoteEndPoint!,
                    cts.Token);
            });

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            var clientStream = client.GetStream();

            // Keep the SSL stream alive until the server side has produced its result.
            SslStream? clientSsl = null;
            Exception? clientError = null;

            try {
                await preTlsClientDriver(clientStream, cts.Token);

                clientSsl = new SslStream(clientStream, false, (_, _, _, _) => true);
                await clientSsl.AuthenticateAsClientAsync(clientSniTargetHost);
            }
            catch (Exception ex) {
                clientError = ex;
            }

            ExchangeSourceInitResult? result;

            try {
                result = await serverTask;
            }
            catch (Exception serverEx) {
                throw new Xunit.Sdk.XunitException(
                    $"Server provider failed (client error: {clientError?.Message ?? "none"}): {serverEx}");
            }
            finally {
                clientSsl?.Dispose();
                listener.Stop();
            }

            if (clientError != null)
                throw clientError;

            return (result, dns);
        }

        private static byte[] Ipv4ConnectRequest(IPAddress ip, int port)
        {
            var request = new byte[] { 0x05, 0x01, 0x00, 0x01, 0, 0, 0, 0, 0, 0 };
            ip.GetAddressBytes().CopyTo(request, 4);
            BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(8), (ushort) port);
            return request;
        }

        private static byte[] DomainConnectRequest(string domain, int port)
        {
            var domainBytes = Encoding.ASCII.GetBytes(domain);
            var request = new byte[4 + 1 + domainBytes.Length + 2];
            request[0] = 0x05;
            request[1] = 0x01;
            request[2] = 0x00;
            request[3] = 0x03;
            request[4] = (byte) domainBytes.Length;
            domainBytes.CopyTo(request, 5);
            BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(5 + domainBytes.Length), (ushort) port);
            return request;
        }

        private static async Task ReadUntilDoubleCrlf(Stream stream, CancellationToken token)
        {
            var single = new byte[1];
            var matched = 0;

            while (matched < 4) {
                var read = await stream.ReadAsync(single, token);

                if (read == 0)
                    throw new EndOfStreamException();

                var expected = matched % 2 == 0 ? (byte) '\r' : (byte) '\n';
                matched = single[0] == expected ? matched + 1 : single[0] == '\r' ? 1 : 0;
            }
        }

        private sealed class TestExchangeContextBuilder : IExchangeContextBuilder
        {
            private readonly FluxzySetting _setting;
            private readonly VariableContext _variableContext = new();

            public TestExchangeContextBuilder(FluxzySetting setting)
            {
                _setting = setting;
            }

            public ValueTask<ExchangeContext> Create(Authority authority, bool secure)
            {
                return new ValueTask<ExchangeContext>(
                    new ExchangeContext(authority, _variableContext, _setting, null!) { Secure = secure });
            }
        }

        private sealed class SpyDnsSolver : IDnsSolver
        {
            public SpyDnsSolver(IPAddress answer)
            {
                Answer = answer;
            }

            public IPAddress Answer { get; }

            public List<string> Queries { get; } = new();

            public Task<IPAddress> SolveDns(string hostName)
            {
                Queries.Add(hostName);
                return Task.FromResult(Answer);
            }
        }
    }
}
