// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core.Pcap;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class SelfCallTests
    {
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
            var allIps = Fluxzy.Misc.IpUtility.GetAllLocalIps()
                               .Select(s => s.AddressFamily == AddressFamily.InterNetworkV6 ?
                                   $"[{s}]" 
                                   : s.ToString())
                               .ToList();

            allIps.Add(Dns.GetHostName());
            allIps.Add("local.fluxzy.io");

            return allIps.Select(s => new object[] { s });
        }
    }
}
