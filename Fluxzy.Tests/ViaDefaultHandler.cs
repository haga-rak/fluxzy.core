// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Core;
using Fluxzy.Interop.Pcap;
using Fluxzy.Interop.Pcap.Cli.Clients;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests
{
    public class ViaDefaultHandler
    {
        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task Get_H2_IIS(SslProvider sslProvider)
        {
            var proxyScope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                a => new OutOfProcessCaptureContext(a));

            await using var tcpProvider = ITcpConnectionProvider.Default; // await CapturedTcpConnectionProvider.Create(proxyScope, false);

            using var handler = new FluxzyDefaultHandler(sslProvider, tcpProvider,
                new DirectoryArchiveWriter(nameof(ViaDefaultHandler), null));

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ"
            );

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task Get_H11_IIS(SslProvider sslProvider)
        {
            var proxyScope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                a => new OutOfProcessCaptureContext(a));

            await using var tcpProvider = ITcpConnectionProvider.Default; // await CapturedTcpConnectionProvider.Create(proxyScope, false);

            using var handler = new FluxzyDefaultHandler(sslProvider, tcpProvider,
                new DirectoryArchiveWriter(nameof(ViaDefaultHandler), null)) {
                Protocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 }
            };

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ"
            );

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task Get_H2_Kestrel(SslProvider sslProvider)
        {
            var proxyScope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                a => new OutOfProcessCaptureContext(a));

            await using var tcpProvider = ITcpConnectionProvider.Default; // await CapturedTcpConnectionProvider.Create(proxyScope, false);

            using var handler = new FluxzyDefaultHandler(sslProvider, tcpProvider,
                new DirectoryArchiveWriter(nameof(ViaDefaultHandler), null));

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://sandbox.smartizy.com:5001/content-produce/1000/1000"
            );

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task Get_H11(SslProvider sslProvider)
        {
            using var handler = new FluxzyDefaultHandler(sslProvider);
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://sandbox.smartizy.com/ip"
            );

            var response = await httpClient.SendAsync(requestMessage);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task Get_ConcurrentDemand(SslProvider sslProvider)
        {
            var urls = new[] {
                "https://sandbox.smartizy.com:5001/content-produce/40000/40000", // H1.1 H2 url
                "https://sandbox.smartizy.com/content-produce/40000/40000" // H1 only url
            };

            using var handler = new FluxzyDefaultHandler(sslProvider);
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            for (var i = 0; i < 50; i++) {
                var requestMessage = new HttpRequestMessage(
                    HttpMethod.Get,
                    urls[i % urls.Length]
                );

                using var response = await httpClient.SendAsync(requestMessage);

                Assert.True(response.IsSuccessStatusCode);
            }
        }
    }
}