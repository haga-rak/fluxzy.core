// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Core;
using Fluxzy.Core.Pcap;
using Fluxzy.Core.Pcap.Cli.Clients;
using Fluxzy.Core.Pcap.Pcapng;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Handlers
{
    [Collection("RUNS_RAW_CAPTURE")]
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
                new EventOnlyArchiveWriter());

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [MemberData(nameof(Get_H11_IIS_Args))]
        public async Task Get_H11_IIS(SslProvider sslProvider, int _)
        {
            using var handler = new FluxzyDefaultHandler(sslProvider) {
                Protocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 }
            };

            using var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular)
            };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://extranet.2befficient.fr/Content/themetr/assets/global/plugins/icheck/skins/all.css"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

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
                new EventOnlyArchiveWriter());

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://sandbox.fluxzy.io:5001/content-produce/1000/1000"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task Get_H11(SslProvider sslProvider)
        {
            using var handler = new FluxzyDefaultHandler(sslProvider);
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://sandbox.fluxzy.io/ip"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task Get_ConcurrentDemand(SslProvider sslProvider)
        {
            var urls = new[] {
                "https://sandbox.fluxzy.io:5001/content-produce/40000/40000", // H1.1 H2 url
                "https://sandbox.fluxzy.io/content-produce/40000/40000", // H1 only url
            };

            using var handler = new FluxzyDefaultHandler(sslProvider);
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Extended) };

            var result = new List<Task<bool>>();

            for (var i = 0; i < 15; i++)
            {
                var requestMessage = new HttpRequestMessage(
                    HttpMethod.Get,
                    urls[i % urls.Length]
                );

                var response = httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                result.Add(response.ContinueWith(t => t.Result.IsSuccessStatusCode));
            }

            var allResult = await Task.WhenAll(result);

            foreach (var b in allResult)
            {
                Assert.True(b);
            }
        }

        public static IEnumerable<object[]> Get_H11_IIS_Args()
        {
            yield return new object[] { SslProvider.BouncyCastle, 0 };

            var count = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? 20 : 1; 

            for (int i = 0; i < count; i++) {
                yield return new object[] { SslProvider.OsDefault, i };
            }
        }

        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        [InlineData(SslProvider.OsDefault)]
        public async Task PcapHandler_With_Pcap(SslProvider sslProvider)
        {
            var idRun = Guid.NewGuid();

            var tempFile = $"Drop/{idRun}.raw.pcapng";
            var decodedFile = $"Drop/{idRun}.decoded.pcapng";

            {
                using var handler = await PcapngUtils.CreateHttpHandler(tempFile, sslProvider: sslProvider);
                using var httpClient = new HttpClient(handler);
                using var _ = await httpClient.GetAsync(TestConstants.TestDomain);
            }

            await using (var outStream = File.Create(decodedFile))
            {
                // Utility to read the pcapng file with the included keys if available
                await PcapngUtils.ReadWithKeysAsync(tempFile).CopyToAsync(outStream);
            }

            Assert.True(File.Exists(tempFile));
            Assert.True(File.Exists(decodedFile));
        }
    }
}
