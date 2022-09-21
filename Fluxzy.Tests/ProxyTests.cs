using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Archiving.Writers;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;
using Header2 = fluxzy.sandbox.models.Header;

namespace Fluxzy.Tests
{
    public class EndToEndTests
    {
        [Theory]
        [InlineData(TestConstants.Http2Host)]
        public async Task Proxy_Receiving_Multiple_Repeating_Header_Value(string host)
        {
            using var proxy = new AddHocProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler, false);

            await Task.WhenAll(Enumerable.Repeat(httpClient, 5)
                .Select(Receiving_Multiple_Repeating_Header_Value_Call));

            await proxy.WaitUntilDone();
        }

        //private static int count = 0; 

        private static async Task Receiving_Multiple_Repeating_Header_Value_Call(HttpClient httpClient)
        {
            int repeatCount = 100;
            var hosts = new[]
            {
                TestConstants.Http2Host, 
                "https://httpmill.smartizy.com:5001"
            };
            var random = new Random(9); 

            var tasks = Enumerable.Repeat(httpClient, repeatCount)
                .Select(async client =>
                {
                    // Interlocked.Increment(ref count);
                    var host = hosts[random.Next(0, hosts.Length)]; 

                    var response = await client.GetAsync($"{host}/headers-random-repeat");
                    var text = await response.Content.ReadAsStringAsync();

                    var items = JsonSerializer.Deserialize<Header2[]>(text
                        , new JsonSerializerOptions()
                        {
                            PropertyNameCaseInsensitive = true
                        })!;

                    var mustBeTrue =
                        items.All(i => response.Headers.Any(
                            t => t.Key == i.Name
                                 && t.Value.Contains(i.Value)));

                    var missing = items.Where(i => !response.Headers.Any(
                        t => t.Key == i.Name
                             && t.Value.Contains(i.Value))).ToList();

                    Assert.True(mustBeTrue);
                });

            await Task.WhenAll(tasks);
        }

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task Proxy_SingleRequest(string host)
        {
            using var proxy = new AddHocProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            await using var randomStream = new RandomDataStream(48, 23632, true);
            await using var hashedStream = new HashedStream(randomStream);

            requestMessage.Content = new StreamContent(hashedStream);

            requestMessage.Headers.Add("X-Test-Header-256", "That value");

            using var response = await httpClient.SendAsync(requestMessage);

            await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response); 

            await proxy.WaitUntilDone(); 
        }


        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task Proxy_SingleRequest_XL(string host)
        {
            using var proxy = new AddHocProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler);

            var bodySize = 16000001;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/content-produce/{bodySize}/{bodySize}");
            
            using var response = await httpClient.SendAsync(requestMessage);

            var stream = await response.Content.ReadAsStreamAsync();

            var length = await stream.Drain(); 

            Assert.Equal(bodySize, length);

            await proxy.WaitUntilDone(); 
        }


        [Theory]
        [InlineData(TestConstants.Http2Host)]
        public async Task Proxy_SingleRequest_WsStatic(string host)
        {
            using var proxy = new AddHocProxy(PortProvider.Next(), 1, 10);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}"),
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://wcpstatic.microsoft.com/mscc/lib/v2/wcp-consent.js");

            using var response = await httpClient.SendAsync(requestMessage);

            var responesString = await response.Content.ReadAsStringAsync();
            

            await proxy.WaitUntilDone(); 
        }

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
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
                $"{host}/global-health-check?dsf=sdfs&dsf=3");

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
            
            var uri = new Uri($"{TestConstants.WssHost}/websocket");
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
            var timeoutSeconds = 500;
            var requestReceived = new TaskCompletionSource<Exchange>();
            var bindHost = "127.0.0.1";
            var bindPort = PortProvider.Next();
            var startupSetting = FluxzySetting
                                 .CreateDefault()
                                 .SetBoundAddress(bindHost, bindPort);

            var (httpClient, proxy) = CreateTestContext(bindHost, bindPort, timeoutSeconds, requestReceived, startupSetting, out var cancellationTokenSource);

            try {

                var response = await httpClient.GetAsync("https://sandbox.smartizy.com/protocol",
                    cancellationTokenSource.Token);

                var responseString = await response.Content.ReadAsStringAsync(
                    cancellationTokenSource.Token);

                Assert.StartsWith("HTTP", responseString);

                await requestReceived.Task;
            }
            finally {
                httpClient.Dispose();
                await proxy.DisposeAsync();
            }
        }

        private static (HttpClient, Proxy p) CreateTestContext(string bindHost, int bindPort, int timeoutSeconds,
            TaskCompletionSource<Exchange> requestReceived, FluxzySetting startupSetting,
            out CancellationTokenSource cancellationTokenSource)
        {
            var messageHandler = new HttpClientHandler() {
                Proxy = new WebProxy($"http://{bindHost}:{bindPort}")
            };

            var httpClient = new HttpClient(messageHandler);

            cancellationTokenSource = new CancellationTokenSource(timeoutSeconds * 1000);

            cancellationTokenSource.Token.Register(() =>
            {
                if (!requestReceived.Task.IsCompleted) {
                    requestReceived.SetException(new Exception("Response not received under {timeoutSeconds} seconds"));
                }
            });

            var proxy = new Proxy(startupSetting,
                new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)));

            proxy.Writer.ExchangeUpdated += delegate(object? sender, ExchangeUpdateEventArgs args)
            {
                if (args.UpdateType == UpdateType.AfterResponseHeader)
                    requestReceived.TrySetResult(args.Original);
            };

            proxy.Run();
            return (httpClient, proxy);
        }

        [Fact]
        public async Task Test_GetThrough_H1_Plain()
        {
            var bindHost = "127.0.0.1";
            var bindPort = PortProvider.Next();
            var timeoutSeconds = 500; 

            var startupSetting = FluxzySetting
                .CreateDefault()
                .SetBoundAddress(bindHost, bindPort);

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
            
            using var proxy = new Proxy(startupSetting,
                new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)));

            proxy.Writer.ExchangeUpdated += delegate (object? sender, ExchangeUpdateEventArgs args)
            {
                if (args.UpdateType == UpdateType.AfterResponseHeader)
                    requestReceived.TrySetResult(args.Original);
            };

            proxy.Run();

            var response = await httpClient.GetAsync("http://info.cern.ch/",
                cancellationTokenSource.Token);

            var responseString = await response.Content.ReadAsStringAsync(
                cancellationTokenSource.Token); 
            
            await requestReceived.Task;
            
        }

        [Fact]
        public async Task Test_GetThrough_H2()
        {
            var bindHost = "127.0.0.1";
            var bindPort = PortProvider.Next();
            var timeoutSeconds = 500; 

            var startupSetting = FluxzySetting
                .CreateDefault()
                .SetBoundAddress(bindHost, bindPort);

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
            

            using var proxy = new Proxy(startupSetting,
                new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)));

            proxy.Writer.ExchangeUpdated += delegate (object? sender, ExchangeUpdateEventArgs args)
            {
                if (args.UpdateType == UpdateType.AfterResponseHeader)
                    requestReceived.TrySetResult(args.Original);
            };

            proxy.Run();

            var response = await httpClient.GetAsync("https://sandbox.smartizy.com:5001/protocol",
                cancellationTokenSource.Token);

            var responseString = await response.Content.ReadAsStringAsync(
                cancellationTokenSource.Token); 

            Assert.Equal("HTTP/2", responseString);

            await requestReceived.Task;
        }
        

        [Fact]
        public async Task Test_GetThrough_Blind_Tunnel()
        {
            var bindHost = "127.0.0.1";
            var bindPort = PortProvider.Next();
            var timeoutSeconds = 500; 

            var startupSetting = FluxzySetting
                .CreateDefault()
                .SetSkipGlobalSslDecryption(true)
                .SetBoundAddress(bindHost, bindPort);

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
            
            using var proxy = new Proxy(startupSetting,
                new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)));

            proxy.Writer.ExchangeUpdated += delegate (object? sender, ExchangeUpdateEventArgs args)
            {
                if (args.UpdateType == UpdateType.AfterResponseHeader)
                    requestReceived.TrySetResult(args.Original);
            };

            proxy.Run();

            var response = await httpClient.GetAsync("https://sandbox.smartizy.com:5001/protocol",
                cancellationTokenSource.Token);

            var responseString = await response.Content.ReadAsStringAsync(
                cancellationTokenSource.Token); 

            Assert.Equal("HTTP/1.1", responseString);

            //await requestReceived.Task;
            
        }
        
        
        
        [Fact]
        public async Task Test_GetThrough_Post()
        {
            var bindHost = "127.0.0.1";
            var bindPort = PortProvider.Next();
            var timeoutSeconds = 500; 

            var startupSetting = FluxzySetting
                .CreateDefault()
                .SetBoundAddress(bindHost, bindPort);

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

            using var proxy = new Proxy(startupSetting,
                new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)));

            proxy.Writer.ExchangeUpdated += delegate (object? sender, ExchangeUpdateEventArgs args)
            {
                if (args.UpdateType == UpdateType.AfterResponseHeader)
                    requestReceived.TrySetResult(args.Original);
            };
            proxy.Run();

            var response = await httpClient.PostAsync("https://sandbox.smartizy.com/content-control/sha256", 
                new StringContent("random posted string", Encoding.UTF8),
                cancellationTokenSource.Token);

            var responseString = await response.Content.ReadAsStringAsync(
                cancellationTokenSource.Token); 

            Assert.True(response.IsSuccessStatusCode);

            await requestReceived.Task;
            
        }

        [Fact]
        public async Task Test_Url_Exceeding_Max_Line()
        {
            var timeoutSeconds = 500;
            var requestReceived = new TaskCompletionSource<Exchange>();
            var bindHost = "127.0.0.1";
            var bindPort = PortProvider.Next();
            var startupSetting = FluxzySetting
                                 .CreateDefault()
                                 .SetBoundAddress(bindHost, bindPort);

            var (httpClient, proxy) = CreateTestContext(bindHost, bindPort, timeoutSeconds, requestReceived, startupSetting, out var cancellationTokenSource);

            try {
                var longSuffix = new string('a', 17 * 1024);
                var response = await httpClient.GetAsync("https://sandbox.smartizy.com:5001/protocol?query=" + longSuffix,
                    cancellationTokenSource.Token);

               await response.Content.ReadAsStringAsync(
                    cancellationTokenSource.Token);

                Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

                await requestReceived.Task;
            }
            finally
            {
                httpClient.Dispose();
                await proxy.DisposeAsync();
            }
        }

    }
}