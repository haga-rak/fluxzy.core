// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Serve
{
    public class H2ServeTests
    {

        [Fact]
        public async Task SimpleH2Request()
        {
            var fluxzySetting = FluxzySetting.CreateLocalRandomPort();

            fluxzySetting.SetServeH2(true);

            await using var proxy = new Proxy(fluxzySetting);
            var endPoints = proxy.Run();

            var client = HttpClientUtility.CreateHttpClient(endPoints, fluxzySetting);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                TestConstants.Http2Host + "/ip");

            using var response = await client.SendAsync(requestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
