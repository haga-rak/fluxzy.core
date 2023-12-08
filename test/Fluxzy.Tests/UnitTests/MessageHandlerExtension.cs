// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.UnitTests
{
    public class MessageHandlerExtension
    {
        [Theory]
        // [InlineData("0.0.0.0")] Disable any address test causing popup on windows
        // [InlineData("::")] Disable any address test causing popup on windows
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
            
            var response = await messageHandler.GetAsync("https://www.example.com");
            Assert.True(response.Headers.Contains("x-test"));
        }
    }
}
