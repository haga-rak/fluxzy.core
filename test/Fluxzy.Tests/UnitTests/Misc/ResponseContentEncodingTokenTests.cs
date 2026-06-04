// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy;
using Fluxzy.Clients.H11;
using Fluxzy.Extensions;
using Fluxzy.Utils.ProcessTracking;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    /// <summary>
    ///     Guards the raw content-encoding token detection used by the pluggable-decoder dispatch.
    ///     Regression: a forwarded <c>Transfer-Encoding: chunked</c> (with no Content-Encoding) must NOT be
    ///     treated as a content codec — otherwise the decode dispatch throws on "chunked" and the proxy
    ///     returns 502 for any chunked response that has a body substitution (e.g. InjectHtmlTagAction,
    ///     TransformResponse). See sandbox.fluxzy.io/protocol which answers chunked.
    /// </summary>
    public class ResponseContentEncodingTokenTests
    {
        [Fact]
        public void Forwarded_chunked_transfer_encoding_is_not_a_content_encoding()
        {
            var exchange = Response(
                new HeaderFieldInfo("content-type".AsMemory(), "text/plain".AsMemory(), forwarded: true),
                new HeaderFieldInfo("transfer-encoding".AsMemory(), "chunked".AsMemory(), forwarded: true));

            Assert.Null(exchange.GetResponseContentEncoding());
            Assert.Equal(CompressionType.None, exchange.GetResponseCompressionType());
        }

        [Theory]
        [InlineData("gzip", "gzip", CompressionType.Gzip)]
        [InlineData("deflate", "deflate", CompressionType.Deflate)]
        [InlineData("br", "br", CompressionType.Brotli)]
        [InlineData("compress", "compress", CompressionType.Compress)]
        [InlineData("zstd", "zstd", CompressionType.None)] // non-native: token surfaced, enum stays None
        [InlineData("ZSTD", "zstd", CompressionType.None)] // lowercased
        public void Content_encoding_token_is_detected(string header, string expectedToken, CompressionType expectedEnum)
        {
            var exchange = Response(
                new HeaderFieldInfo("content-encoding".AsMemory(), header.AsMemory(), forwarded: true));

            Assert.Equal(expectedToken, exchange.GetResponseContentEncoding());
            Assert.Equal(expectedEnum, exchange.GetResponseCompressionType());
        }

        [Fact]
        public void Identity_content_encoding_is_treated_as_no_encoding()
        {
            var exchange = Response(
                new HeaderFieldInfo("content-encoding".AsMemory(), "identity".AsMemory(), forwarded: true));

            Assert.Null(exchange.GetResponseContentEncoding());
        }

        [Fact]
        public void Forwarded_gzip_transfer_encoding_is_still_detected()
        {
            // gzip carried by a forwarded Transfer-Encoding remains detectable (original behavior).
            var exchange = Response(
                new HeaderFieldInfo("transfer-encoding".AsMemory(), "gzip".AsMemory(), forwarded: true));

            Assert.Equal("gzip", exchange.GetResponseContentEncoding());
        }

        private static FakeResponseExchange Response(params HeaderFieldInfo[] headers) => new(headers);

        private sealed class FakeResponseExchange : IExchange
        {
            private readonly List<HeaderFieldInfo> _responseHeaders;

            public FakeResponseExchange(IEnumerable<HeaderFieldInfo> responseHeaders)
            {
                _responseHeaders = responseHeaders.ToList();
            }

            public IEnumerable<HeaderFieldInfo>? GetResponseHeaders() => _responseHeaders;

            public int Id => 1;
            public string FullUrl => "https://sandbox.fluxzy.io/protocol";
            public string KnownAuthority => "sandbox.fluxzy.io";
            public int KnownPort => 443;
            public string HttpVersion => "HTTP/1.1";
            public string Method => "GET";
            public string Path => "/protocol";
            public int StatusCode => 200;
            public string? EgressIp => null;
            public string? Comment => null;
            public HashSet<Tag>? Tags => null;
            public bool IsWebSocket => false;
            public List<WsMessage>? WebSocketMessages => null;
            public Agent? Agent => null;
            public ProcessInfo? ProcessInfo => null;
            public List<ClientError> ClientErrors => new();

            public IEnumerable<HeaderFieldInfo> GetRequestHeaders() => Array.Empty<HeaderFieldInfo>();
            public IEnumerable<HeaderFieldInfo>? GetRequestTrailers() => null;
            public IEnumerable<HeaderFieldInfo>? GetResponseTrailers() => null;
        }
    }
}
