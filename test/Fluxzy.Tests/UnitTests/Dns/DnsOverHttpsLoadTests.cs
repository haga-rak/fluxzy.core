// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Dns
{
    public class DnsOverHttpsLoadTests
    {
        [Fact]
        public async Task Validate()
        {
            // Arrange
            var fluxzySetting = FluxzySetting.CreateLocalRandomPort();

            fluxzySetting.ConfigureRule()
                         .WhenAny()
                         .Do(new UseDnsOverHttpsAction("CLOUDFLARE"));

            fluxzySetting.UseBouncyCastleSslEngine();
            fluxzySetting.SkipRemoteCertificateValidation = true;

            await using var proxy = new Proxy(fluxzySetting);

            var endPoints = proxy.Run();

            var httpClient = HttpClientUtility.CreateHttpClient(endPoints, fluxzySetting);

            var count = 0;

            async Task<bool> DoRequest()
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Head, $"https://nothing-{count++}-{Guid.NewGuid().ToString()}.yahoo.fr/favicon.ico");

                // Act
                using var response = await httpClient.SendAsync(requestMessage);

                var res = response.StatusCode < (HttpStatusCode)500;

                if (!res)
                {
                    var fullResponseMessage = await response.Content.ReadAsStringAsync();
                }

                return res;
            }

            var tasks = new List<Task<bool>>();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(DoRequest());
            }

            var results = await Task.WhenAll(tasks);

            Assert.All(results, Assert.True);
        }
    }
}

