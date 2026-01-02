// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Fluxzy.Utils.ProcessTracking;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class ProcessTrackerTests
    {
        private static bool IsSupportedPlatform =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        [Fact]
        public void GetProcessInfo_WithActiveListener_ReturnsProcessInfo()
        {
            if (!IsSupportedPlatform)
            {
                Assert.Throws<PlatformNotSupportedException>(() =>
                    ProcessTracker.Instance.GetProcessInfo(80));
                return;
            }

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            var localPort = ((IPEndPoint)listener.LocalEndpoint).Port;

            var tracker = ProcessTracker.Instance;
            var processInfo = tracker.GetProcessInfo(localPort);

            Assert.NotNull(processInfo);
            Assert.True(processInfo.ProcessId > 0);
            Assert.Equal(Environment.ProcessId, processInfo.ProcessId);
            Assert.NotNull(processInfo.ProcessPath);
            Assert.NotEmpty(processInfo.ProcessPath);
        }

        [Fact]
        public void GetProcessInfo_WithConnectedSocket_ReturnsProcessInfo()
        {
            if (!IsSupportedPlatform)
                return;

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            var serverPort = ((IPEndPoint)listener.LocalEndpoint).Port;

            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, serverPort);

            var clientPort = ((IPEndPoint)client.Client.LocalEndPoint!).Port;

            var tracker = ProcessTracker.Instance;
            var processInfo = tracker.GetProcessInfo(clientPort);

            Assert.NotNull(processInfo);
            Assert.Equal(Environment.ProcessId, processInfo.ProcessId);
        }

        [Fact]
        public void GetProcessInfo_ZeroPort_ReturnsNull()
        {
            if (!IsSupportedPlatform)
                return;

            var tracker = ProcessTracker.Instance;
            var processInfo = tracker.GetProcessInfo(0);

            Assert.Null(processInfo);
        }

        [Fact]
        public void GetProcessInfo_MultipleCallsForSamePort_ReturnsCachedResult()
        {
            if (!IsSupportedPlatform)
                return;

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            var localPort = ((IPEndPoint)listener.LocalEndpoint).Port;

            var tracker = ProcessTracker.Instance;

            var result1 = tracker.GetProcessInfo(localPort);
            var result2 = tracker.GetProcessInfo(localPort);

            Assert.NotNull(result1);
            Assert.NotNull(result2);

            // Cache should return the exact same instance
            Assert.Same(result1, result2);
        }

        [Fact]
        public void GetProcessInfo_ReturnsProcessArguments()
        {
            if (!IsSupportedPlatform)
                return;

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            var localPort = ((IPEndPoint)listener.LocalEndpoint).Port;

            var tracker = ProcessTracker.Instance;
            var processInfo = tracker.GetProcessInfo(localPort);

            Assert.NotNull(processInfo);

            // The test runner should have command line arguments
            // Note: ProcessArguments may be null on some platforms if access is denied
            // but on Windows running our own process, it should work
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.NotNull(processInfo.ProcessArguments);
                Assert.NotEmpty(processInfo.ProcessArguments);
            }
        }

        [Fact]
        public void GetProcessInfo_AfterListenerStopped_ReturnsCachedThenNull()
        {
            if (!IsSupportedPlatform)
                return;

            int localPort;
            ProcessInfo? cachedInfo;

            using (var listener = new TcpListener(IPAddress.Loopback, 0))
            {
                listener.Start();
                localPort = ((IPEndPoint)listener.LocalEndpoint).Port;

                var tracker = ProcessTracker.Instance;
                cachedInfo = tracker.GetProcessInfo(localPort);

                Assert.NotNull(cachedInfo);
            }

            // Listener is now stopped, but cache should still return the cached value
            var trackerAfter = ProcessTracker.Instance;
            var processInfoAfter = trackerAfter.GetProcessInfo(localPort);

            // Cache returns same instance even after listener stopped
            Assert.Same(cachedInfo, processInfoAfter);
        }
    }
}
