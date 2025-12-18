// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;

namespace Fluxzy.Tests._Fixtures
{
    public static class TestConstants
    {
        public const string Http11Host = "https://sandbox.fluxzy.io";
        public const string Http2Host = "https://sandbox.fluxzy.io:5001";
        public const string PlainHttp11 = "http://sandbox.fluxzy.io:8899";
        public const string WssHost = "wss://sandbox.fluxzy.io:5001";

        public const string HttpBinHost = "registry.2befficient.io:40300";
        public const string HttpBinHostDomainOnly  = "registry.2befficient.io";

        public const string TestDomain = "https://sandbox.fluxzy.io/ip";
        public const string TestDomainHost = "sandbox.fluxzy.io";
        public const string TestDomainPage = "https://sandbox.fluxzy.io/swagger/index.html";

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
