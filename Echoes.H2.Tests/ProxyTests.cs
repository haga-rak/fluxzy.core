using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Core;
using Xunit;

namespace Echoes.H2.Tests
{
    public class ProxyTests
    {
        static ProxyTests()
        {
            Environment.SetEnvironmentVariable("Echoes_EnableNetworkFileDump", "true");
        }

        [Fact]
        public async Task Test_GetThrough_H1()
        {
            var bindHost = "127.0.0.1";
            var bindPort = 14212;
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
            var bindPort = 14213;
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
            var bindPort = 14214;
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