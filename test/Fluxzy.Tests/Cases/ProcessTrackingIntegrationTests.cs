// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Utils.ProcessTracking;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class ProcessTrackingIntegrationTests
    {
        private static bool IsSupportedPlatform =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Checks if ProcessTracker can identify processes on this system.
        /// ProcessTracker may require elevated privileges on some platforms.
        /// </summary>
        private static bool CanProcessTrackerIdentifyCurrentProcess()
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var localPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                var processInfo = ProcessTracker.Instance.GetProcessInfo(localPort);
                return processInfo != null && processInfo.ProcessId == Environment.ProcessId;
            }
            catch
            {
                return false;
            }
        }

        [Fact]
        public async Task ProcessTracking_Enabled_ShouldCaptureProcessInfo()
        {
            if (!IsSupportedPlatform)
                return;

            // Skip if ProcessTracker cannot work on this system (e.g., insufficient permissions)
            if (!CanProcessTrackerIdentifyCurrentProcess())
                return;

            // Arrange
            await using var proxy = new AddHocProxy(
                expectedRequestCount: 1,
                timeoutSeconds: 30,
                configureSetting: setting => setting.SetEnableProcessTracking(true));

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}")
            };

            using var httpClient = new HttpClient(clientHandler);

            // Act
            var response = await httpClient.GetAsync(TestConstants.Http11Host);
            response.EnsureSuccessStatusCode();

            await proxy.WaitUntilDone();

            // Assert
            var exchange = proxy.CapturedExchanges.FirstOrDefault();
            Assert.NotNull(exchange);
            Assert.NotNull(exchange.ProcessInfo);
            Assert.True(exchange.ProcessInfo.ProcessId > 0);
            Assert.Equal(Environment.ProcessId, exchange.ProcessInfo.ProcessId);
            Assert.NotNull(exchange.ProcessInfo.ProcessPath);
            Assert.NotEmpty(exchange.ProcessInfo.ProcessPath);
        }

        [Fact]
        public async Task ProcessTracking_Disabled_ShouldNotCaptureProcessInfo()
        {
            if (!IsSupportedPlatform)
                return;

            // Arrange - process tracking disabled by default
            await using var proxy = new AddHocProxy(
                expectedRequestCount: 1,
                timeoutSeconds: 30);

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}")
            };

            using var httpClient = new HttpClient(clientHandler);

            // Act
            var response = await httpClient.GetAsync(TestConstants.Http11Host);
            response.EnsureSuccessStatusCode();

            await proxy.WaitUntilDone();

            // Assert
            var exchange = proxy.CapturedExchanges.FirstOrDefault();
            Assert.NotNull(exchange);
            Assert.Null(exchange.ProcessInfo);
        }

        [Fact]
        public async Task ProcessTracking_ExplicitlyDisabled_ShouldNotCaptureProcessInfo()
        {
            if (!IsSupportedPlatform)
                return;

            // Arrange
            await using var proxy = new AddHocProxy(
                expectedRequestCount: 1,
                timeoutSeconds: 30,
                configureSetting: setting => setting.SetEnableProcessTracking(false));

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{proxy.BindHost}:{proxy.BindPort}")
            };

            using var httpClient = new HttpClient(clientHandler);

            // Act
            var response = await httpClient.GetAsync(TestConstants.Http11Host);
            response.EnsureSuccessStatusCode();

            await proxy.WaitUntilDone();

            // Assert
            var exchange = proxy.CapturedExchanges.FirstOrDefault();
            Assert.NotNull(exchange);
            Assert.Null(exchange.ProcessInfo);
        }
    }
}
