// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections;
using System.Collections.Generic;

namespace Fluxzy.Tests._Fixtures
{
    public static class TestConstants
    {
        public const string Http11Host = "https://sandbox.smartizy.com";
        public const string Http2Host = "https://sandbox.smartizy.com:5001";
        public const string PlainHttp11 = "http://sandbox.smartizy.com:8899";
        public const string WssHost = "wss://sandbox.smartizy.com:5001";

        public const string HttpBinHost = "registry.2befficient.io:40300";
        public const string HttpBinHostDomainOnly  = "registry.2befficient.io";

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

        public static IEnumerable<object[]> GetHosts()
        {
            yield return new object[] { Http11Host };
            yield return new object[] { Http2Host };
            yield return new object[] { PlainHttp11 };
        }
    }
}
