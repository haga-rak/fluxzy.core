// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Tests.Cli.Scaffolding;
using Fluxzy.Tests.Common;
using Xunit;

namespace Fluxzy.Tests
{
    public class ErrorCases
    {
        [Theory]
        [InlineData(TestConstants.Http2Host)]
        public async Task Connection_Close_Before_Response(string host)
        {
            await using var proxy = new AddHocProxy();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/connection-broken-abort-before-response");

            try {
                using var response = await httpClient.SendAsync(requestMessage);

                var responseBody = await response.Content.ReadAsStringAsync();

                Assert.Equal((HttpStatusCode) 528, response.StatusCode);
                Assert.True(!string.IsNullOrWhiteSpace(responseBody));
            }
            catch (HttpRequestException) {
                // May reached here 
            }
        }

        //TESTER SUR Sandbox UN ENVOI D ENTETE MULTIPLE POUR FORCER LE DYNAMIC TableAttribute UPDATE
        //    [Theory]
        [InlineData(TestConstants.Http2Host)]
        public async Task LargeHeaderFieldValue(string host)
        {
            await using var proxy = new AddHocProxy();

            using var clientHandler = new HttpClientHandler {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/ip");

            requestMessage.Headers.Add("x-lar-value", new string('v', 4096));
            requestMessage.Headers.Add("x-lar-value", new string('z', 4096));

            using var response = await httpClient.SendAsync(requestMessage);

            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.True(!string.IsNullOrWhiteSpace(responseBody));
        }

        //[Fact]
        //public async Task Connection_RefusedTcplevel()
        //{
        //    using var proxy = new AddHocProxy();

        //    using var clientHandler = new HttpClientHandler
        //    {
        //        Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
        //    };

        //    using var httpClient = new HttpClient(clientHandler);

        //    var requestMessage = new HttpRequestMessage(HttpMethod.Get,
        //        $"https://sandbox.smartizy.com:4988/");

        //    using var response = await httpClient.SendAsync(requestMessage);

        //    var responseBody = await response.Content.ReadAsStringAsync(); 

        //    Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        //    Assert.True(!string.IsNullOrWhiteSpace(responseBody));
        //}

        [Theory]
        [InlineData("zehjz\r\nsffsfsfq\r\n\r\n")]
        [InlineData("\r\n\r\n")]
        [InlineData("GET /index.html HTTP/1.1\r\ndsfsdf\r\n\r\n")]
        public async Task Run_Cli_Handle_Invalid_Http_Requests_From_Client(string rawCommunication)
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1/0";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();

            using var tcpClient = new TcpClient();

            await tcpClient.ConnectAsync("127.0.0.1", fluxzyInstance.ListenPort);

            var networkStream = tcpClient.GetStream();

            networkStream.Write(Encoding.UTF8.GetBytes(rawCommunication));
            networkStream.ReadByte();
        }
    }
}
