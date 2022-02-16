using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Core;
using Echoes.H2.Tests.Tools;
using Echoes.H2.Tests.Utils;
using Xunit;

namespace Echoes.H2.Tests
{
    public class ProxyTests
    {
        private const string Http11Host = "sandbox.smartizy.com";
        private const string Http2Host = "sandbox.smartizy.com:5001";

        static ProxyTests()
        {
             Environment.SetEnvironmentVariable("Echoes_EnableNetworkFileDump", "true");
        }
        
        [Theory]
        [InlineData(Http11Host)]
        [InlineData(Http2Host)]
        public async Task Proxy_SingleRequest(string host)
        {
            using var proxy = new AddHocProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://{host}/global-health-check?dsf=sdfs&dsf=3");

            await using var randomStream = new RandomDataStream(48, 23632, true);
            await using var hashedStream = new HashedStream(randomStream);

            requestMessage.Content = new StreamContent(hashedStream);

            requestMessage.Headers.Add("X-Test-Header-256", "That value");

            using var response = await httpClient.SendAsync(requestMessage);

            await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response); 

            await proxy.WaitUntilDone(); 
        }

        [Theory]
        [InlineData(Http11Host)]
        [InlineData(Http2Host)]
        public async Task Proxy_MultipleRequest(string host)
        {
            int concurrentCount = 15; 

            using var proxy = new AddHocProxy(PortProvider.Next(), concurrentCount, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler);

            await Task.WhenAll(Enumerable.Range(0, concurrentCount)
                .Select(
                (index) => PerformRequest(host, index, httpClient))); 

            await proxy.WaitUntilDone(); 
        }

        private static async Task PerformRequest(string host, int i, HttpClient httpClient)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                $"https://{host}/global-health-check?dsf=sdfs&dsf=3");

            await using var randomStream = new RandomDataStream(48, 23632, true);
            await using var hashedStream = new HashedStream(randomStream);

            requestMessage.Content = new StreamContent(hashedStream);
            requestMessage.Headers.Add("X-Identifier", $"{i}-{Guid.NewGuid()}");

            using var response = await httpClient.SendAsync(requestMessage);

            await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response);
        }

        [Fact]
        public async Task Proxy_WebSockets()
        {
            var message = Encoding.ASCII.GetBytes("Hello world!");

            using var proxy = new AddHocProxy(PortProvider.Next(), 1, 10);

            using ClientWebSocket ws = new()
            {
                Options = { Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}") }
            }; 
            
            var uri = new Uri("wss://sandbox.smartizy.com:5001/websocket");
            Memory<byte> buffer = new byte[4096];

            await ws.ConnectAsync(uri, CancellationToken.None);
            await ws.ReceiveAsync(buffer, CancellationToken.None); 

            var hash = Convert.ToBase64String(SHA1.HashData(message));

            await ws.SendAsync(message, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage,
                CancellationToken.None);

            var res = await ws.ReceiveAsync(buffer, CancellationToken.None);

            var resultHash = Encoding.ASCII.GetString(buffer.Slice(0, res.Count).Span);

            Assert.Equal(hash, resultHash);
        }

        [Fact]
        public async Task Test_GetThrough_H1()
        {
            var bindHost = "127.0.0.1";
            var bindPort = PortProvider.Next();
            var timeoutSeconds = 500; 

            var startupSetting = ProxyStartupSetting
                .CreateDefault()
                .SetAsSystemProxy(false)
                .SetBoundAddress(bindHost)
                .SetListenPort(bindPort);

            var messageHandler = new HttpClientHandler()
            {
                Proxy = new WebProxy($"http://{bindHost}:{bindPort}")
            };

            var httpClient = new HttpClient(messageHandler); 

            var requestReceived = new TaskCompletionSource<Exchange>();
            var cancellationTokenSource = new CancellationTokenSource(timeoutSeconds * 1000);

            cancellationTokenSource.Token.Register(() =>
            {
                if (!requestReceived.Task.IsCompleted)
                {
                    requestReceived.SetException(new Exception("Response not received under {timeoutSeconds} seconds"));
                }
            });

            Task OnNewExchange(Exchange ex)
            {
                requestReceived.SetResult(ex);
                return Task.CompletedTask;
            }

            using var proxy = new Proxy(startupSetting,
                new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)),
                OnNewExchange);

            proxy.Run();

            var response = await httpClient.GetAsync("https://sandbox.smartizy.com/protocol",
                cancellationTokenSource.Token);

            var responseString = await response.Content.ReadAsStringAsync(
                cancellationTokenSource.Token); 

            Assert.StartsWith("HTTP", responseString);

            await requestReceived.Task;
            
        }

        [Fact]
        public async Task Test_GetThrough_H2()
        {
            var bindHost = "127.0.0.1";
            var bindPort = PortProvider.Next();
            var timeoutSeconds = 500; 

            var startupSetting = ProxyStartupSetting
                .CreateDefault()
                .SetAsSystemProxy(false)
                .SetBoundAddress(bindHost)
                .SetListenPort(bindPort);

            var messageHandler = new HttpClientHandler()
            {
                Proxy = new WebProxy($"http://{bindHost}:{bindPort}")
            };

            var httpClient = new HttpClient(messageHandler); 

            var requestReceived = new TaskCompletionSource<Exchange>();
            var cancellationTokenSource = new CancellationTokenSource(timeoutSeconds * 1000);

            cancellationTokenSource.Token.Register(() =>
            {
                if (!requestReceived.Task.IsCompleted)
                {
                    requestReceived.SetException(new Exception("Response not received under {timeoutSeconds} seconds"));
                }
            });

            Task OnNewExchange(Exchange ex)
            {
                requestReceived.SetResult(ex);
                return Task.CompletedTask;
            }

            using var proxy = new Proxy(startupSetting,
                new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)),
                OnNewExchange);

            proxy.Run();

            var response = await httpClient.GetAsync("https://sandbox.smartizy.com:5001/protocol",
                cancellationTokenSource.Token);

            var responseString = await response.Content.ReadAsStringAsync(
                cancellationTokenSource.Token); 

            Assert.Equal("HTTP/2", responseString);

            await requestReceived.Task;
            
        }
        
        [Fact]
        public async Task Test_GetThrough_Post()
        {
            var bindHost = "127.0.0.1";
            var bindPort = PortProvider.Next();
            var timeoutSeconds = 500; 

            var startupSetting = ProxyStartupSetting
                .CreateDefault()
                .SetAsSystemProxy(false)
                .SetBoundAddress(bindHost)
                .SetListenPort(bindPort);

            var messageHandler = new HttpClientHandler()
            {
                Proxy = new WebProxy($"http://{bindHost}:{bindPort}")
            };

            var httpClient = new HttpClient(messageHandler); 

            var requestReceived = new TaskCompletionSource<Exchange>();
            var cancellationTokenSource = new CancellationTokenSource(timeoutSeconds * 1000);

            cancellationTokenSource.Token.Register(() =>
            {
                if (!requestReceived.Task.IsCompleted)
                {
                    requestReceived.SetException(new Exception("Response not received under {timeoutSeconds} seconds"));
                }
            });

            Task OnNewExchange(Exchange ex)
            {
                requestReceived.SetResult(ex);
                return Task.CompletedTask;
            }

            using var proxy = new Proxy(startupSetting,
                new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)),
                OnNewExchange);

            proxy.Run();

            var response = await httpClient.PostAsync("https://sandbox.smartizy.com/content-control/sha256", 
                new StringContent("random posted string", Encoding.UTF8),
                cancellationTokenSource.Token);

            var responseString = await response.Content.ReadAsStringAsync(
                cancellationTokenSource.Token); 

            Assert.True(response.IsSuccessStatusCode);

            await requestReceived.Task;
            
        }
        
    }
}