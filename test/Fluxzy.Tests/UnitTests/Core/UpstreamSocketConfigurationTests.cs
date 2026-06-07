// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class UpstreamSocketConfigurationTests
    {
        [Fact]
        public async Task Callback_Receives_Unconnected_Socket_And_Resolved_Endpoint()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            var acceptTask = listener.AcceptTcpClientAsync();

            UpstreamSocketContext? captured = null;
            var connectedAtCallback = true;

            var options = new UpstreamConnectOptions("example.com", port, ctx => {
                captured = ctx;
                connectedAtCallback = ctx.Socket.Connected;
            });

            await using var connection = ITcpConnectionProvider.Default.Create(string.Empty);
            var result = await connection.ConnectAsync(IPAddress.Loopback, port, options);

            Assert.NotNull(captured);
            Assert.False(connectedAtCallback);
            Assert.Equal("example.com", captured!.RequestedHost);
            Assert.Equal(port, captured.RequestedPort);
            Assert.Equal(port, captured.RemoteEndPoint.Port);
            Assert.Equal(IPAddress.Loopback, captured.RemoteEndPoint.Address);
            Assert.Equal(AddressFamily.InterNetwork, captured.Socket.AddressFamily);

            await result.Stream.DisposeAsync();
            (await acceptTask).Dispose();
            listener.Stop();
        }

        [Fact]
        public async Task Throwing_Callback_Surfaces_As_ClientError()
        {
            var options = new UpstreamConnectOptions("example.com", 443,
                _ => throw new InvalidOperationException("boom"));

            await using var connection = ITcpConnectionProvider.Default.Create(string.Empty);

            var error = await Assert.ThrowsAsync<ClientErrorException>(
                () => connection.ConnectAsync(IPAddress.Loopback, 65000, options));

            Assert.Equal(NetworkErrorCodes.UpstreamConfigurationFailed, error.ClientError.NetworkErrorCode);
        }
    }
}
