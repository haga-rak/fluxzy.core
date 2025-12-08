// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class Extended528Tests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_Expired_Ssl(bool useBouncyCastle)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.UseBouncyCastle = useBouncyCastle;

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var httpClient = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await httpClient.GetAsync("https://expired.badssl.com/");

             _ = await response.Content.ReadAsStringAsync();

            Assert.Equal(528, (int) response.StatusCode);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_Wrong_Host(bool useBouncyCastle)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.UseBouncyCastle = useBouncyCastle;

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var httpClient = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await httpClient.GetAsync("https://wrong.host.badssl.com/");

             _ = await response.Content.ReadAsStringAsync();

            Assert.Equal(528, (int) response.StatusCode);
        }
    }
}
