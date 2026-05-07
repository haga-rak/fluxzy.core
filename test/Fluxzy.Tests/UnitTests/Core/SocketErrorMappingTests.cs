// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Sockets;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class SocketErrorMappingTests
    {
        [Theory]
        [InlineData(SocketError.ConnectionRefused, NetworkErrorCodes.ConnectionRefused)]
        [InlineData(SocketError.ConnectionReset, NetworkErrorCodes.ConnectionReset)]
        [InlineData(SocketError.ConnectionAborted, NetworkErrorCodes.ConnectionAborted)]
        [InlineData(SocketError.Shutdown, NetworkErrorCodes.ConnectionReset)]
        [InlineData(SocketError.TimedOut, NetworkErrorCodes.ConnectionTimeout)]
        [InlineData(SocketError.HostUnreachable, NetworkErrorCodes.HostUnreachable)]
        [InlineData(SocketError.NetworkUnreachable, NetworkErrorCodes.NetworkUnreachable)]
        [InlineData(SocketError.HostNotFound, NetworkErrorCodes.DnsNotFound)]
        [InlineData(SocketError.NoData, NetworkErrorCodes.DnsNoData)]
        [InlineData(SocketError.TryAgain, NetworkErrorCodes.DnsTryAgain)]
        [InlineData(SocketError.AccessDenied, NetworkErrorCodes.Unknown)]
        [InlineData(SocketError.Fault, NetworkErrorCodes.Unknown)]
        public void MapSocketError_Returns_Expected_Token(SocketError code, string expected)
        {
            var (_, token) = ConnectionErrorHandler.MapSocketError(code, "1.2.3.4", 443);
            Assert.Equal(expected, token);
        }

        [Theory]
        [InlineData(SocketError.ConnectionRefused)]
        [InlineData(SocketError.ConnectionReset)]
        [InlineData(SocketError.ConnectionAborted)]
        [InlineData(SocketError.TimedOut)]
        [InlineData(SocketError.HostUnreachable)]
        [InlineData(SocketError.NetworkUnreachable)]
        [InlineData(SocketError.HostNotFound)]
        [InlineData(SocketError.NoData)]
        [InlineData(SocketError.TryAgain)]
        public void MapSocketError_Returns_NonEmpty_Message(SocketError code)
        {
            var (message, _) = ConnectionErrorHandler.MapSocketError(code, "1.2.3.4", 443);
            Assert.False(string.IsNullOrWhiteSpace(message));
        }
    }
}
