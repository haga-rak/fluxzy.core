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
using Fluxzy.Tests.UnitTests.Ssl;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    /// <summary>
    ///     Provider-level coverage of RecoverHostNameFromSni: authority rewrite + IP pin over SOCKS5,
    ///     transparent CONNECT and blind tunnel, and that the pinned IP keeps upstream off the resolver
    ///     for the provisional exchange and every per-request exchange after it.
    /// </summary>
    public class SniHostRecoveryProviderTests
    {
        private static readonly IPAddress TargetIp = IPAddress.Parse("1.2.3.4");
        private const int TargetPort = 443;

        [Fact]
        public async Task Socks5_IpTarget_WithHostnameSni_RewritesAuthorityAndPinsIp()
        {
            var run = await RunSocks5(
                recoverHostNameFromSni: true, blindMode: false,
                target: Ipv4ConnectRequest(TargetIp, TargetPort), TlsHandshake("example.com"));

            var exchange = AssertResult(run.Result);

            Assert.Equal("example.com", exchange.Authority.HostName);
            Assert.Equal(TargetPort, exchange.Authority.Port);
            Assert.True(exchange.Authority.Secure);
            Assert.Equal(TargetIp, exchange.Context.RemoteHostIp);
        }

        [Fact]
        public async Task Socks5_IpTarget_WithoutUsableSni_KeepsIpAuthorityAndDoesNotPin()
        {
            var run = await RunSocks5(
                recoverHostNameFromSni: true, blindMode: false,
                target: Ipv4ConnectRequest(TargetIp, TargetPort), TlsHandshake(TargetIp.ToString()));

            var exchange = AssertResult(run.Result);

            Assert.Equal(TargetIp.ToString(), exchange.Authority.HostName);
            Assert.Null(exchange.Context.RemoteHostIp);
        }

        [Fact]
        public async Task Socks5_HostnameTarget_IsNeverRewritten()
        {
            var run = await RunSocks5(
                recoverHostNameFromSni: true, blindMode: false,
                target: DomainConnectRequest("example.org", TargetPort), TlsHandshake("example.com"));

            var exchange = AssertResult(run.Result);

            Assert.Equal("example.org", exchange.Authority.HostName);
            Assert.Null(exchange.Context.RemoteHostIp);
        }

        [Fact]
        public async Task Connect_IpTarget_WithHostnameSni_RewritesAuthorityAndPinsIp()
        {
            var run = await RunConnect(
                recoverHostNameFromSni: true, connectTarget: $"{TargetIp}:{TargetPort}",
                TlsHandshake("example.com"));

            var exchange = AssertResult(run.Result);

            Assert.Equal("example.com", exchange.Authority.HostName);
            Assert.Equal(TargetPort, exchange.Authority.Port);
            Assert.True(exchange.Authority.Secure);
            Assert.Equal(TargetIp, exchange.Context.RemoteHostIp);
        }

        // Blind tunnel: never decrypted, but the sniffed SNI still names the authority and pins the IP.
        [Fact]
        public async Task Socks5_BlindMode_IpTarget_RecordsHostnameAndPinsIp()
        {
            var run = await RunSocks5(
                recoverHostNameFromSni: true, blindMode: true,
                target: Ipv4ConnectRequest(TargetIp, TargetPort), RawClientHello("blind.example.com"));

            var exchange = AssertResult(run.Result);

            Assert.Equal("blind.example.com", exchange.Authority.HostName);
            Assert.Equal(TargetIp, exchange.Context.RemoteHostIp);
        }

        [Fact]
        public async Task Recovery_PinnedIp_KeepsUpstreamOffTheDnsResolver()
        {
            var run = await RunSocks5(
                recoverHostNameFromSni: true, blindMode: false,
                target: Ipv4ConnectRequest(TargetIp, TargetPort), TlsHandshake("example.com"));

            var exchange = AssertResult(run.Result);
            Assert.Equal("example.com", exchange.Authority.HostName);
            Assert.Equal(TargetIp, exchange.Context.RemoteHostIp);

            var resolution = await DnsUtility.ComputeDnsUpdateExchange(
                exchange, ITimingProvider.Default, run.Dns, null);

            Assert.Equal(TargetIp, resolution.EndPoint.Address);
            Assert.Equal(TargetPort, resolution.EndPoint.Port);
            Assert.Empty(run.Dns.Queries);
        }

        [Fact]
        public async Task WithoutRecovery_UpstreamGoesThroughTheDnsResolver()
        {
            var run = await RunSocks5(
                recoverHostNameFromSni: false, blindMode: false,
                target: Ipv4ConnectRequest(TargetIp, TargetPort), TlsHandshake("example.com"));

            var exchange = AssertResult(run.Result);
            Assert.Equal(TargetIp.ToString(), exchange.Authority.HostName);
            Assert.Null(exchange.Context.RemoteHostIp);

            var resolution = await DnsUtility.ComputeDnsUpdateExchange(
                exchange, ITimingProvider.Default, run.Dns, null);

            Assert.Equal(run.Dns.Answer, resolution.EndPoint.Address);
            Assert.Contains(TargetIp.ToString(), run.Dns.Queries);
        }

        // The provisional CONNECT exchange never drives an upstream connection; the pin matters on the
        // per-request exchanges produced by the downstream pipe after the TLS upgrade.
        [Fact]
        public async Task Socks5_PerRequestExchange_KeepsRecoveredHostAndIpPin()
        {
            var run = await RunSocks5(
                recoverHostNameFromSni: true, blindMode: false,
                target: Ipv4ConnectRequest(TargetIp, TargetPort),
                TlsHandshakeThenRequest("example.com"),
                readNextExchange: true);

            Assert.NotNull(run.NextExchange);
            Assert.Equal("example.com", run.NextExchange!.Authority.HostName);
            Assert.Equal(TargetIp, run.NextExchange.Context.RemoteHostIp);

            var resolution = await DnsUtility.ComputeDnsUpdateExchange(
                run.NextExchange, ITimingProvider.Default, run.Dns, null);

            Assert.Equal(TargetIp, resolution.EndPoint.Address);
            Assert.Empty(run.Dns.Queries);
        }

        // The exchange context must be built with the recovered authority, so rules evaluated at
        // OnAuthorityReceived scope (blind mode, skip decryption...) match the host, not the IP.
        [Fact]
        public async Task Recovery_AuthorityScopeRules_SeeTheRecoveredHost()
        {
            var run = await RunSocks5(
                recoverHostNameFromSni: true, blindMode: false,
                target: Ipv4ConnectRequest(TargetIp, TargetPort), TlsHandshake("example.com"));

            var authority = Assert.Single(run.ContextBuilder.SeenAuthorities);
            Assert.Equal("example.com", authority.HostName);
        }

        // A rule that forces a remote IP at authority scope (e.g. SpoofDnsAction) wins over the pin.
        [Fact]
        public async Task Recovery_RuleForcedRemoteIp_IsNotOverriddenByThePin()
        {
            var ruleIp = IPAddress.Parse("5.6.7.8");

            var run = await RunSocks5(
                recoverHostNameFromSni: true, blindMode: false,
                target: Ipv4ConnectRequest(TargetIp, TargetPort), TlsHandshake("example.com"),
                ruleForcedRemoteIp: ruleIp);

            var exchange = AssertResult(run.Result);

            Assert.Equal("example.com", exchange.Authority.HostName);
            Assert.Equal(ruleIp, exchange.Context.RemoteHostIp);
        }

        private static Exchange AssertResult(ExchangeSourceInitResult? result)
        {
            Assert.NotNull(result);
            return result!.ProvisionalExchange;
        }

        // Complete a real TLS handshake against the proxy with the given SNI.
        private static Func<Stream, CancellationToken, Task<IDisposable?>> TlsHandshake(string targetHost)
        {
            return async (clientStream, _) => {
                var ssl = new SslStream(clientStream, false, (_, _, _, _) => true);
                await ssl.AuthenticateAsClientAsync(targetHost);
                return ssl; // kept alive until the server side produced its result
            };
        }

        // Handshake, then send a request so the pipe can produce a per-request exchange.
        private static Func<Stream, CancellationToken, Task<IDisposable?>> TlsHandshakeThenRequest(string targetHost)
        {
            return async (clientStream, token) => {
                var ssl = new SslStream(clientStream, false, (_, _, _, _) => true);
                await ssl.AuthenticateAsClientAsync(targetHost);

                var request = Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {targetHost}\r\n\r\n");
                await ssl.WriteAsync(request, token);
                await ssl.FlushAsync(token);

                return ssl;
            };
        }

        // Just emit a ClientHello carrying the SNI (blind path: the proxy only sniffs and tunnels).
        private static Func<Stream, CancellationToken, Task<IDisposable?>> RawClientHello(string serverName)
        {
            return async (clientStream, token) => {
                await clientStream.WriteAsync(TlsClientHelloParserTests.BuildClientHello(serverName), token);
                return null;
            };
        }

        private static async Task<RunResult> RunSocks5(
            bool recoverHostNameFromSni, bool blindMode, byte[] target,
            Func<Stream, CancellationToken, Task<IDisposable?>> clientTlsAction,
            bool readNextExchange = false, IPAddress? ruleForcedRemoteIp = null)
        {
            return await RunProvider(recoverHostNameFromSni, blindMode,
                (updater, idProvider, contextBuilder, dnsSolver) =>
                    new Socks5SourceProvider(updater, idProvider, NoAuthenticationMethod.Instance,
                        contextBuilder, dnsSolver, recoverHostNameFromSni),
                async (clientStream, token) => {
                    await clientStream.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, token);
                    await clientStream.ReadExactlyAsync(new byte[2], token); // method selection
                    await clientStream.WriteAsync(target, token);
                    await clientStream.ReadExactlyAsync(new byte[10], token); // IPv4 reply
                },
                clientTlsAction, readNextExchange, ruleForcedRemoteIp);
        }

        private static async Task<RunResult> RunConnect(
            bool recoverHostNameFromSni, string connectTarget,
            Func<Stream, CancellationToken, Task<IDisposable?>> clientTlsAction,
            bool readNextExchange = false, IPAddress? ruleForcedRemoteIp = null)
        {
            return await RunProvider(recoverHostNameFromSni, blindMode: false,
                (updater, idProvider, contextBuilder, _) =>
                    new FromProxyConnectSourceProvider(updater, idProvider, NoAuthenticationMethod.Instance,
                        contextBuilder, recoverHostNameFromSni),
                async (clientStream, token) => {
                    var connect = Encoding.ASCII.GetBytes(
                        $"CONNECT {connectTarget} HTTP/1.1\r\nHost: {connectTarget}\r\n\r\n");
                    await clientStream.WriteAsync(connect, token);
                    await ReadUntilDoubleCrlf(clientStream, token);
                },
                clientTlsAction, readNextExchange, ruleForcedRemoteIp);
        }

        private static async Task<RunResult> RunProvider(
            bool recoverHostNameFromSni, bool blindMode,
            Func<SecureConnectionUpdater, IIdProvider, IExchangeContextBuilder, IDnsSolver, ExchangeSourceProvider>
                providerFactory,
            Func<Stream, CancellationToken, Task> preTlsClientDriver,
            Func<Stream, CancellationToken, Task<IDisposable?>> clientTlsAction,
            bool readNextExchange = false, IPAddress? ruleForcedRemoteIp = null)
        {
            var root = Certificate.UseDefault();
            using var certProvider = new CertificateProvider(root, new InMemoryCertificateCache());
            var setting = FluxzySetting.CreateLocalRandomPort().SetRecoverHostNameFromSni(recoverHostNameFromSni);

            var updater = new SecureConnectionUpdater(certProvider, serveH2: false);
            var dns = new SpyDnsSolver(IPAddress.Parse("9.9.9.9"));

            var contextBuilder = new TestExchangeContextBuilder(setting, blindMode) {
                RuleForcedRemoteHostIp = ruleForcedRemoteIp
            };

            var provider = providerFactory(updater, new FromIndexIdProvider(0, 0), contextBuilder, dns);

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var serverTask = Task.Run(async () => {
                using var serverConnection = await listener.AcceptTcpClientAsync(cts.Token);
                using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(1024 * 8);

                var initResult = await provider.InitClientConnection(
                    serverConnection.GetStream(), buffer,
                    (IPEndPoint) serverConnection.Client.LocalEndPoint!,
                    (IPEndPoint) serverConnection.Client.RemoteEndPoint!,
                    cts.Token);

                Exchange? nextExchange = null;

                if (readNextExchange && initResult != null) {
                    using var exchangeScope = new ExchangeScope();

                    nextExchange = await initResult.DownStreamPipe
                                                   .ReadNextExchange(buffer, exchangeScope, cts.Token);
                }

                return (InitResult: initResult, NextExchange: nextExchange);
            });

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            var clientStream = client.GetStream();

            IDisposable? keepAlive = null;
            Exception? clientError = null;

            try {
                await preTlsClientDriver(clientStream, cts.Token);
                keepAlive = await clientTlsAction(clientStream, cts.Token);
            }
            catch (Exception ex) {
                clientError = ex;
            }

            (ExchangeSourceInitResult? InitResult, Exchange? NextExchange) serverOutcome;

            try {
                serverOutcome = await serverTask;
            }
            catch (Exception serverEx) {
                throw new Xunit.Sdk.XunitException(
                    $"Server provider failed (client error: {clientError?.Message ?? "none"}): {serverEx}");
            }
            finally {
                keepAlive?.Dispose();
                listener.Stop();
            }

            if (clientError != null)
                throw clientError;

            return new RunResult(serverOutcome.InitResult, dns, contextBuilder, serverOutcome.NextExchange);
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

        private sealed record RunResult(
            ExchangeSourceInitResult? Result,
            SpyDnsSolver Dns,
            TestExchangeContextBuilder ContextBuilder,
            Exchange? NextExchange);

        private sealed class TestExchangeContextBuilder : IExchangeContextBuilder
        {
            private readonly FluxzySetting _setting;
            private readonly bool _blindMode;
            private readonly VariableContext _variableContext = new();

            public TestExchangeContextBuilder(FluxzySetting setting, bool blindMode)
            {
                _setting = setting;
                _blindMode = blindMode;
            }

            /// <summary>Authorities passed to Create, i.e. what authority-scope rules would see.</summary>
            public List<Authority> SeenAuthorities { get; } = new();

            /// <summary>Simulates a rule forcing a remote IP at OnAuthorityReceived scope.</summary>
            public IPAddress? RuleForcedRemoteHostIp { get; init; }

            public ValueTask<ExchangeContext> Create(Authority authority, bool secure)
            {
                SeenAuthorities.Add(authority);

                return new ValueTask<ExchangeContext>(
                    new ExchangeContext(authority, _variableContext, _setting, null!) {
                        Secure = secure,
                        BlindMode = _blindMode,
                        RemoteHostIp = RuleForcedRemoteHostIp
                    });
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
