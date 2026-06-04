// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Fluxzy;
using Fluxzy.Clients.H11;
using Fluxzy.Extensions;
using Fluxzy.Misc.Streams;
using Fluxzy.Utils.ProcessTracking;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    /// <summary>
    ///     Diagnosis of "InjectHtmlTagAction can't inject" on pages like promovacances.com.
    ///
    ///     The matcher itself is fine (see <see cref="InjectHtmlHeadReproTests" />). The injection only ever
    ///     runs on a *decoded* body (<c>ExchangeContext.GetSubstitutedResponseBody</c> calls
    ///     <c>CompressionHelper.GetDecodedStream(compressionType, ...)</c> first). The compression type is
    ///     derived from the `Content-Encoding` header by <c>ExchangeExtensions.GetResponseCompressionType</c>,
    ///     which only understands gzip / deflate / compress / brotli.
    ///
    ///     fluxzy advertises `Accept-Encoding: gzip, deflate, br, zstd` (see ImpersonateProfileManager), so a
    ///     CDN may legitimately answer with `Content-Encoding: zstd`. That value maps to
    ///     <see cref="CompressionType.None" />, the body is therefore NOT decompressed, and
    ///     <see cref="InjectStreamOnStream" /> ends up scanning raw zstd bytes — it never finds `&lt;head&gt;`,
    ///     so the body is streamed through unchanged and nothing is injected.
    /// </summary>
    public class InjectHtmlTagZstdDiagnosisTests
    {
        private const string Html =
            "<html lang=\"fr\">\n<head><script>var x = 1;</script></head>\n<body>hello</body></html>";

        // 1. The mapping bug: a `zstd` response is reported as "not compressed", so it is never decoded.
        [Theory]
        [InlineData("gzip", CompressionType.Gzip)]
        [InlineData("br", CompressionType.Brotli)]
        [InlineData("deflate", CompressionType.Deflate)]
        [InlineData("zstd", CompressionType.None)]   // <-- unsupported: treated as "no compression"
        public void ContentEncoding_detection(string contentEncoding, CompressionType expected)
        {
            var exchange = new FakeResponseExchange(
                new HeaderFieldInfo("content-type", "text/html; charset=utf-8"),
                new HeaderFieldInfo("content-encoding", contentEncoding));

            Assert.Equal(expected, exchange.GetResponseCompressionType());
        }

        // 2. The consequence: feeding the *compressed* (== undecoded zstd) body to the injector finds no
        //    <head> and injects nothing; feeding the *decoded* body injects correctly.
        [Fact]
        public void Injection_fails_on_compressed_body_but_succeeds_on_decoded_body()
        {
            var raw = Encoding.UTF8.GetBytes(Html);

            // gzip stands in for "a content-encoding the proxy negotiated"; for zstd the proxy can't decode
            // it at all, so the substitution stream receives these still-compressed bytes.
            var compressed = Gzip(raw);

            var onCompressed = Inject(compressed);
            var onDecoded = Inject(raw);

            // The marker never lands when the body is still compressed -> "can't inject".
            Assert.DoesNotContain("INJECTED", onCompressed, StringComparison.Ordinal);

            // Same input, but decoded first -> injection works, right after <head>.
            Assert.Contains("<head><!--INJECTED-->", onDecoded, StringComparison.Ordinal);
        }

        private static string Inject(byte[] body)
        {
            var matcher = new SimpleHtmlTagOpeningMatcher(Encoding.UTF8, StringComparison.OrdinalIgnoreCase, false);

            using var stream = new InjectStreamOnStream(
                new MemoryStream(body),
                matcher,
                Encoding.UTF8.GetBytes("head"),
                new MemoryStream(Encoding.UTF8.GetBytes("<!--INJECTED-->")));

            // Read raw bytes (the body may not be valid UTF-8 when compressed) then latin1-decode for the assert.
            using var output = new MemoryStream();
            var buffer = new byte[64];
            int read;

            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) {
                output.Write(buffer, 0, read);
            }

            return Encoding.Latin1.GetString(output.ToArray());
        }

        private static byte[] Gzip(byte[] data)
        {
            using var memory = new MemoryStream();

            using (var gzip = new GZipStream(memory, CompressionLevel.Optimal, true)) {
                gzip.Write(data, 0, data.Length);
            }

            return memory.ToArray();
        }

        /// <summary>
        ///     Minimal <see cref="IExchange" /> exposing only the response headers needed by
        ///     <see cref="ExchangeExtensions.GetResponseCompressionType" />.
        /// </summary>
        private sealed class FakeResponseExchange : IExchange
        {
            private readonly List<HeaderFieldInfo> _responseHeaders;

            public FakeResponseExchange(params HeaderFieldInfo[] responseHeaders)
            {
                _responseHeaders = responseHeaders.ToList();
            }

            public IEnumerable<HeaderFieldInfo>? GetResponseHeaders() => _responseHeaders;

            public int Id => 1;
            public string FullUrl => "https://www.promovacances.com/";
            public string KnownAuthority => "www.promovacances.com";
            public int KnownPort => 443;
            public string HttpVersion => "HTTP/2";
            public string Method => "GET";
            public string Path => "/";
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
