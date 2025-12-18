// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Events
{
    /// <summary>
    ///  Following tests will fail if example.com fails to respond
    /// </summary>
    public class EventWriterTests
    {
        [Theory]
        [InlineData(ArchiveUpdateType.BeforeRequestHeader)]
        [InlineData(ArchiveUpdateType.AfterResponseHeader)]
        [InlineData(ArchiveUpdateType.AfterResponse)]
        public async Task ExchangeUpdated_ArchiveUpdateStatus(ArchiveUpdateType updateType)
        {
            var maxTimeoutSeconds = TimeoutConstants.Short; 
            var fluxzySetting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);

            await using (var proxy = new Proxy(fluxzySetting)) {
                var taskCompletionSource = new TaskCompletionSource(); 

                proxy.Writer.ExchangeUpdated += (_, args) => {
                    
                    if (args.UpdateType == updateType)
                        taskCompletionSource.SetResult();
                };

                var endPoint = proxy.Run().First();

                using var client = HttpClientHelper.Create(endPoint);

                await client.GetAsync(TestConstants.TestDomain);

                var delayTask = Task.Delay(maxTimeoutSeconds * 1000);

                await Task.WhenAny(taskCompletionSource.Task, delayTask);
                
                Assert.False(delayTask.IsCompletedSuccessfully);
            }
        }

        [Fact]
        public async Task ExchangeUpdated_Control_Content()
        {
            var maxTimeoutSeconds = TimeoutConstants.Short; 
            var fluxzySetting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            var url = "https://sandbox.fluxzy.io/global-health-check"; 

            await using (var proxy = new Proxy(fluxzySetting)) {
                var taskCompletionSource = new TaskCompletionSource();

                ExchangeInfo? exchangeInfo = null; 

                proxy.Writer.ExchangeUpdated += (_, args) => {
                    
                    if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                        taskCompletionSource.SetResult();

                    exchangeInfo = args.ExchangeInfo; 
                };

                var endPoint = proxy.Run().First();

                using var client = HttpClientHelper.Create(endPoint);

                await client.GetAsync(url);

                var delayTask = Task.Delay(maxTimeoutSeconds * 1000);

                await Task.WhenAny(taskCompletionSource.Task, delayTask);
                
                Assert.False(delayTask.IsCompletedSuccessfully);
                Assert.NotNull(exchangeInfo); 
                Assert.Equal(200, exchangeInfo.StatusCode); 
                Assert.Equal(url, exchangeInfo.FullUrl); 
            }
        }

        [Fact]
        public async Task ConnectionUpdated_Control_Content()
        {
            var maxTimeoutSeconds = TimeoutConstants.Short; 
            var fluxzySetting = FluxzySetting.CreateDefault(IPAddress.Loopback, 0);
            var url = "https://sandbox.fluxzy.io/global-health-check"; 

            await using (var proxy = new Proxy(fluxzySetting)) {
                var taskCompletionSource = new TaskCompletionSource();
                ConnectionInfo? connectionInfo = null; 

                proxy.Writer.ConnectionUpdated += (_, args) => {
                   connectionInfo = args.Connection;
                   taskCompletionSource.SetResult();
                };

                var endPoint = proxy.Run().First();

                using var client = HttpClientHelper.Create(endPoint);

                await client.GetAsync(url);

                var delayTask = Task.Delay(maxTimeoutSeconds * 1000);

                await Task.WhenAny(taskCompletionSource.Task, delayTask);
                
                Assert.False(delayTask.IsCompletedSuccessfully);
                Assert.NotNull(connectionInfo); 
                Assert.Equal(1, proxy.Writer.TotalProcessedExchanges); 
            }
        }
    }



    internal static class HttpClientHelper
    {
        public static HttpClient Create(IPEndPoint proxyEndPoint, Action<HttpClientHandler> ? configureHandler = null)
        {
            var httpClientHandler = new HttpClientHandler {
                Proxy = new WebProxy(proxyEndPoint.Address.ToString(), proxyEndPoint.Port),
                UseProxy = true
            };

            if (configureHandler != null) {

                configureHandler(httpClientHandler);
            }
            
            return new HttpClient(httpClientHandler);
        }
    }
}
