// Copyright © 2022 Haga Rakotoharivelo

using System;

namespace Fluxzy.Tests.Common
{
    public static class TestConstants
    {
        public const string Http11Host = "https://sandbox.smartizy.com";
        public const string Http2Host = "https://sandbox.smartizy.com:5001";
        public const string Http2Host2 = "https://sandbox.smartizy.com:4430";
        public const string WssHost = "wss://sandbox.smartizy.com:5001";

        public static string GetHost(string protocol)
        {
            if (protocol == "http11")
                return Http11Host;

            if (protocol == "http2")
                return Http2Host;

            throw new ArgumentException(nameof(protocol), "Unknown protocol");
        }
    }
}
