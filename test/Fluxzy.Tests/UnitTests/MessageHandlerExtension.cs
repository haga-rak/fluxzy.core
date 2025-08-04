// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests
{
    public class MessageHandlerExtension
    {
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("::1")]
        public async Task Test_Create_Http_Client(string rawAddress)
        {
            var boundAddress = IPAddress.Parse(rawAddress);
            var startSetting = FluxzySetting.CreateDefault(boundAddress, 0); 
            
            startSetting.ConfigureRule()
                        .WhenAny().Do(new AddResponseHeaderAction("x-test", "test"));
            
            await using var proxy = new Proxy(startSetting);

            var endPoints = proxy.Run();

            var messageHandler = HttpClientUtility.CreateHttpClient(endPoints, startSetting); 
            
            var response = await messageHandler.GetAsync(TestConstants.TestDomain);
            Assert.True(response.Headers.Contains("x-test"));
        }
    }
}
