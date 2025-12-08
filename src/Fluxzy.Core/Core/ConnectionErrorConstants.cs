// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core
{
    internal static class ConnectionErrorConstants
    {
        public static readonly string Generic502 =
            "HTTP/1.1 528 Fluxzy transport error\r\n" +
            "x-fluxzy: Fluxzy transport error\r\n" +
            "Content-length: {0}\r\n" +
            "Content-type: text/plain\r\n" +
            "Connection : close\r\n\r\n";
    }
}
