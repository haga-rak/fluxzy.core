using System;
using System.Net;
using System.Net.Http;
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
        public async Task Test_GetThrough()
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
        
    }
}