// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Events
{
    /// <summary>
    ///  Following tests will fail if example.com fails to respond
    /// </summary>
    public class EventWriterTests
    {
        [Fact]
        public async void ExchangeUpdated_Complete()
        {
            var maxTimeoutSeconds = TimeoutConstants.Short; 
            var fluxzySetting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);

            await using (var proxy = new Proxy(fluxzySetting)) {
                var endPoint = proxy.Run().First();
                var taskCompletionSource = new TaskCompletionSource(); 

                proxy.Writer.ExchangeUpdated += (_, args) => {
                    
                    if (args.UpdateType == ArchiveUpdateType.Complete)
                        taskCompletionSource.SetResult();
                };

                using var client = HttpClientHelper.Create(endPoint);

                await client.GetAsync("https://example.com"); 
                
                await Task.WhenAny(taskCompletionSource.Task,
                    Task.Delay(maxTimeoutSeconds * 1000));
                
                Assert.True(taskCompletionSource.Task.IsCompleted);
            }
        }
    }

    internal static class HttpClientHelper
    {
        public static HttpClient Create(IPEndPoint proxyEndPoint)
        {
            var httpClientHandler = new HttpClientHandler {
                Proxy = new WebProxy(proxyEndPoint.Address.ToString(), proxyEndPoint.Port),
                UseProxy = true
            };

            return new HttpClient(httpClientHandler);
        }
    }
}
