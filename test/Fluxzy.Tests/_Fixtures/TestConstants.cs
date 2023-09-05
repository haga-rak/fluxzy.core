// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Tests._Fixtures
{
    public static class TestConstants
    {
        public const string Http11Host = "https://sandbox.smartizy.com";
        public const string Http2Host = "https://sandbox.smartizy.com:5001";
        public const string PlainHttp11 = "http://sandbox.smartizy.com:8899";
        public const string Http2Host2 = "https://sandbox.smartizy.com:4430";
        public const string WssHost = "wss://sandbox.smartizy.com:5001";
        public const string HttpBinHost = "registry.2befficient.io:40300";
        public static string HttpBinHostDomainOnly  => HttpBinHost.Split(':')[0];

        public static string GetHost(string protocol)
        {
            if (protocol.StartsWith("http11"))
                return Http11Host;

            if (protocol.StartsWith("http2"))
                return Http2Host;

            if (protocol.StartsWith("plainhttp11"))
                return PlainHttp11;

            throw new ArgumentException(nameof(protocol), "Unknown protocol");
        }
    }
}
