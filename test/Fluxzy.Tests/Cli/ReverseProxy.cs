// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class ReverseProxy
    {
        [Theory]
        [MemberData(nameof(GetArguments))]
        public async Task Validate(string stringUri, string method)
        {
            var uri = new Uri(stringUri);

            var mode = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                ? "ReverseSecure"
                : "ReversePlain";

            // Arrange
            var commandLine = $"start -l 127.0.0.1:0 --no-cert-cache  --mode-reverse-port {uri.Port} --mode {mode} ";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();

            var handler = ReverseProxyHelper.GetSpoofedHandler(fluxzyInstance.ListenPort,
                uri.Host, uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));

            var httpClient = new HttpClient(handler);

            var requestMessage = new HttpRequestMessage(new HttpMethod(method), stringUri);

            using var response = await httpClient.SendAsync(requestMessage);

            Assert.NotEqual(528, (int) response.StatusCode);
        }

        public static IEnumerable<object[]> GetArguments()
        {
            var url = new[] {
                $"{TestConstants.PlainHttp11}/global-health-check",
                $"{TestConstants.Http11Host}/global-health-check",
                $"{TestConstants.Http2Host}/global-health-check",
                "http://eu.httpbin.org/",
                TestConstants.TestDomain
            };

            var testedMethods = new[] {
                "get", "post"
            };

            foreach (var uri in url) {
                foreach (var method in testedMethods) {
                    yield return new object[] { uri, method };
                }
            }
        }
    }
}
