// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core
{
    internal static class ProxyConstants
    {
        public static string AcceptTunnelResponseString { get; } =
            $"HTTP/1.1 200 OK\r\n" +
            $"x-fluxzy-message: enjoy your privacy!\r\n" +
            $"Content-length: 0\r\n" +
            $"Connection: keep-alive\r\n" +
            $"Keep-alive: timeout=5\r\n" +
            $"\r\n";
    }
}
