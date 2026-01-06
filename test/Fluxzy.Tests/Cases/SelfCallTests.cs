// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class SelfCallTests
    {
        [Fact]
        public void IncludeAndroidEmulatorHost_DefaultValue_IsTrue()
        {
            // The default value for IncludeAndroidEmulatorHost should be true
            var setting = FluxzySetting.CreateLocalRandomPort();
            Assert.True(setting.IncludeAndroidEmulatorHost);
        }

        [Fact]
        public void IncludeAndroidEmulatorHost_CanBeDisabled()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetIncludeAndroidEmulatorHost(false);
            Assert.False(setting.IncludeAndroidEmulatorHost);
        }

        [Fact]
        public void AndroidEmulatorHostAddress_IsCorrect()
        {
            // Verify the Android emulator host address is 10.0.2.2
            Assert.Equal(IPAddress.Parse("10.0.2.2"), IsSelfFilter.AndroidEmulatorHostAddress);
        }

        [Fact]
        public void IsSelfFilter_WithAndroidEmulatorEnabled_MatchesEmulatorAddress()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetIncludeAndroidEmulatorHost(true);

            var authority = new Authority("10.0.2.2", 8080, false);
            var variableContext = new VariableContext();
            var exchangeContext = new ExchangeContext(authority, variableContext, setting, null!) {
                RemoteHostIp = IPAddress.Parse("10.0.2.2"),
                RemoteHostPort = 8080
            };

            // Create a minimal exchange for testing
            var exchange = CreateTestExchange(8080);

            var filter = new IsSelfFilter();

            // Act
            var result = filter.Apply(exchangeContext, authority, exchange, null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSelfFilter_WithAndroidEmulatorDisabled_DoesNotMatchEmulatorAddress()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetIncludeAndroidEmulatorHost(false);

            var authority = new Authority("10.0.2.2", 8080, false);
            var variableContext = new VariableContext();
            var exchangeContext = new ExchangeContext(authority, variableContext, setting, null!) {
                RemoteHostIp = IPAddress.Parse("10.0.2.2"),
                RemoteHostPort = 8080
            };

            // Create a minimal exchange for testing
            var exchange = CreateTestExchange(8080);

            var filter = new IsSelfFilter();

            // Act
            var result = filter.Apply(exchangeContext, authority, exchange, null);

            // Assert
            Assert.False(result);
        }

        private static Exchange CreateTestExchange(int port)
        {
            var authority = new Authority("10.0.2.2", port, false);
            var requestHeader = "GET / HTTP/1.1\r\nHost: 10.0.2.2\r\n\r\n".AsMemory();
            var exchange = new Exchange(IIdProvider.FromZero, authority, requestHeader, "HTTP/1.1", System.DateTime.UtcNow);
            exchange.Metrics.DownStreamLocalPort = port;
            return exchange;
        }

        [Theory]
        [MemberData(nameof(AllValidHosts))]
        public async Task MakeMultipleSelfCall(string host)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            
            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();
            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            for (int i = 0; i < 4; i++) {
                var response = await client.GetAsync($"http://{host}:{endPoints.First().Port}/welcome");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
            }
        }

        [Theory]
        [MemberData(nameof(AllValidHosts))]
        public async Task MakeMultipleSelfCallCa(string host)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            
            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();
            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var certificate = setting.CaCertificate.GetX509Certificate();
            var certificateString = Encoding.UTF8.GetString(certificate.ExportToPem());

            for (int i = 0; i < 5; i++) {
                var response = await client.GetAsync($"http://{host}:{endPoints.First().Port}/ca");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal(certificateString, content);
            }
        }

        [Theory]
        [MemberData(nameof(AllValidHosts))]
        public async Task MakeMultipleSelfCallNothing(string host)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();
            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            for (int i = 0; i < 4; i++)
            {
                var response = await client.GetAsync($"http://{host}:{endPoints.First().Port}/notmounted");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
            }
        }

        [Theory]
        [MemberData(nameof(AllValidHosts))]
        public async Task MakeCustomHookFilter(string host)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule()
                   .When(new FilterCollection(new IsSelfFilter(), new PathFilter("/hello")))
                   .ReplyText("Hello");

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();
            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            for (int i = 0; i < 4; i++)
            {
                var response = await client.GetAsync($"http://{host}:{endPoints.First().Port}/hello");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal("Hello", content);
            }
        }

        public static IEnumerable<object[]> AllValidHosts()
        {
            var allIps = new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }
                               .Select(s => s.AddressFamily == AddressFamily.InterNetworkV6 ?
                                   $"[{s}]" 
                                   : s.ToString())
                               .ToList();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                allIps.Add(Dns.GetHostName());

            allIps.Add("local.fluxzy.io");

            return allIps.Select(s => new object[] { s });
        }
    }
}
