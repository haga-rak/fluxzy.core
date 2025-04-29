// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class ExtendedRootDomainTests
    {
        [Theory]
        [InlineData("https://tfl.gov.uk", "CN=*.tfl.gov.uk")]
        [InlineData("https://www.fluxzy.io", "CN=*.fluxzy.io")]
        [InlineData("https://fluxzy.io", "CN=*.fluxzy.io")]
        public async Task Test_Regular(string url, string cn)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var resultSubject = "";

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting, 
                configureHandler: handler => {
                    handler.ServerCertificateCustomValidationCallback = (a, b, chain, c) => {
                        resultSubject = b?.Subject;
                        return true;
                    };
                });

            _ = await client.GetAsync(url);

            Assert.Equal(cn, resultSubject);
        }

        [Theory]
        [InlineData("https://tfl.gov.uk", "CN=*.tfl.gov.uk")]
        [InlineData("https://www.fluxzy.io", "CN=*.fluxzy.io")]
        [InlineData("https://fluxzy.io", "CN=*.fluxzy.io")]
        public async Task Test_NoCache(string url, string cn)
        {
            FluxzySharedSetting.NoCacheOnFqdn = true;

            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var resultSubject = "";

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting,
                configureHandler: handler => {
                    handler.ServerCertificateCustomValidationCallback = (a, b, chain, c) => {
                        resultSubject = b?.Subject;
                        return true;
                    };
                });

            _ = await client.GetAsync(url);

            Assert.Equal(cn, resultSubject);
        }
    }
}
