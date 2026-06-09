// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Fluxzy.Rules;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    /// <summary>
    ///     Covers RecoverHostNameFromSni: when the connect authority is an IP, the leaf certificate
    ///     is named from the TLS SNI sent by the client, falling back to the IP CN otherwise.
    /// </summary>
    public class SecureConnectionUpdaterSniTests
    {
        private const string ConnectIp = "93.184.216.34";

        [Fact]
        public async Task Recover_On_WithHostnameSni_NamesLeafFromSni()
        {
            var (served, observedSni) = await RunHandshake(
                recoverHostNameFromSni: true, connectHost: ConnectIp, clientTargetHost: "example.com");

            Assert.Equal("CN=*.example.com", served.Subject);

            var dnsNames = served.Extensions
                                 .OfType<X509SubjectAlternativeNameExtension>()
                                 .Single()
                                 .EnumerateDnsNames()
                                 .ToList();

            Assert.Contains("example.com", dnsNames);
            Assert.Contains("*.example.com", dnsNames);
            Assert.Equal("example.com", observedSni);
        }

        [Fact]
        public async Task Recover_On_WithoutUsableSni_FallsBackToIpCn()
        {
            // The client targets the IP itself, so there is no usable hostname SNI: the IP CN is kept.
            var (served, observedSni) = await RunHandshake(
                recoverHostNameFromSni: true, connectHost: ConnectIp, clientTargetHost: ConnectIp);

            Assert.Equal($"CN={ConnectIp}", served.Subject);
            Assert.Null(observedSni);
        }

        [Fact]
        public async Task Recover_Off_IgnoresSni_KeepsIpCn()
        {
            // Default behavior: even with a hostname SNI, the leaf is named from the IP authority.
            var (served, observedSni) = await RunHandshake(
                recoverHostNameFromSni: false, connectHost: ConnectIp, clientTargetHost: "example.com");

            Assert.Equal($"CN={ConnectIp}", served.Subject);
            Assert.Null(observedSni);
        }

        private static async Task<(X509Certificate2 Served, string? ObservedSni)> RunHandshake(
            bool recoverHostNameFromSni, string connectHost, string clientTargetHost)
        {
            var root = Certificate.UseDefault();
            using var provider = new CertificateProvider(root, new InMemoryCertificateCache());
            var setting = FluxzySetting.CreateLocalRandomPort();

            var updater = new SecureConnectionUpdater(provider, serveH2: false,
                recoverHostNameFromSni: recoverHostNameFromSni);

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var serverTask = Task.Run(async () => {
                using var serverConnection = await listener.AcceptTcpClientAsync(cts.Token);
                var authority = new Authority(connectHost, port, true);
                var context = new ExchangeContext(authority, new VariableContext(), setting, null!);

                return await updater.AuthenticateAsServer(
                    serverConnection.GetStream(), connectHost, context, cts.Token);
            });

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);

            using var clientSsl = new SslStream(client.GetStream(), false, (_, _, _, _) => true);
            await clientSsl.AuthenticateAsClientAsync(clientTargetHost);

            var result = await serverTask;
            var served = (X509Certificate2) clientSsl.RemoteCertificate!;

            listener.Stop();

            return (served, result.SniHost);
        }
    }
}
