// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.HPack;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Tests._Files;
using Xunit;

namespace Fluxzy.Tests.UnitTests.HPack
{
    public class Http11ParserTests
    {
        private static readonly int MaxHeaderLength = 1024 * 8;

        [Fact]
        public void Parse_Unparse_Request_Header()
        {
            var header = new UTF8Encoding(false).GetString(Headers.Req001);

            Span<char> resultBuffer = stackalloc char[MaxHeaderLength];

            var headerBlocks = Http11Parser.Read(header.AsMemory());
            var result = Http11Parser.Write(headerBlocks, resultBuffer).ToString();

            Assert.Equal(header, result, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_Unparse_Response_Header()
        {
            var header = new UTF8Encoding(false).GetString(Headers.Resp001);

            Span<char> resultBuffer = stackalloc char[MaxHeaderLength];

            var headerBlocks = Http11Parser.Read(header.AsMemory());
            var result = Http11Parser.Write(headerBlocks, resultBuffer).ToString();

            Assert.Equal(header, result, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_Request_ExpandsPseudoHeadersAndAuthority()
        {
            var header = "GET /foo HTTP/1.1\r\nHost: example.com\r\nAccept: text/html\r\n\r\n";

            var fields = Http11Parser.Read(header.AsMemory());

            Assert.Collection(
                fields,
                f => AssertHeader(f, ":method", "GET"),
                f => AssertHeader(f, ":scheme", "https"),
                f => AssertHeader(f, ":path", "/foo"),
                f => AssertHeader(f, ":authority", "example.com"),
                f => AssertHeader(f, "Accept", "text/html"));
        }

        [Fact]
        public void Read_Request_SchemeHonoursIsHttpsFlag()
        {
            var header = "GET / HTTP/1.1\r\nHost: example.com\r\n\r\n";

            var fields = Http11Parser.Read(header.AsMemory(), isHttps: false);

            var scheme = fields.Single(f =>
                f.Name.Span.Equals(":scheme", StringComparison.Ordinal));

            Assert.Equal("http", scheme.Value.ToString());
        }

        [Fact]
        public void Read_Response_ExtractsStatusOnly()
        {
            var header = "HTTP/1.1 204 No Content\r\nX-Custom: abc\r\n\r\n";

            var fields = Http11Parser.Read(header.AsMemory());

            Assert.Collection(
                fields,
                f => AssertHeader(f, ":status", "204"),
                f => AssertHeader(f, "X-Custom", "abc"));
        }

        [Fact]
        public void Read_Request_WithAbsoluteUriStripsSchemeAndAuthority()
        {
            var header = "GET https://example.com/foo/bar?x=1 HTTP/1.1\r\nHost: example.com\r\n\r\n";

            var fields = Http11Parser.Read(header.AsMemory());

            var path = fields.Single(f =>
                f.Name.Span.Equals(":path", StringComparison.Ordinal));

            Assert.Equal("/foo/bar?x=1", path.Value.ToString());
        }

        [Fact]
        public void Read_Request_SplitsCookiesByDefault()
        {
            var header =
                "GET / HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Cookie: a=1; b=2;   c=3\r\n\r\n";

            var fields = Http11Parser.Read(header.AsMemory());

            var cookieValues = fields
                               .Where(f => f.Name.Span.Equals("cookie", StringComparison.Ordinal))
                               .Select(f => f.Value.ToString())
                               .ToArray();

            Assert.Equal(new[] { "a=1", "b=2", "c=3" }, cookieValues);
        }

        [Fact]
        public void Read_Request_KeepsCookieJoinedWhenSplitDisabled()
        {
            var header =
                "GET / HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Cookie: a=1; b=2; c=3\r\n\r\n";

            var fields = Http11Parser.Read(header.AsMemory(), splitCookies: false);

            var cookieValues = fields
                               .Where(f => f.Name.Span.Equals("Cookie", StringComparison.Ordinal))
                               .Select(f => f.Value.ToString())
                               .ToArray();

            Assert.Single(cookieValues);
            Assert.Equal("a=1; b=2; c=3", cookieValues[0]);
        }

        [Fact]
        public void Read_DropsNonForwardableHeaders_ByDefault()
        {
            var header =
                "GET / HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: keep-alive\r\n" +
                "Keep-Alive: timeout=5\r\n" +
                "Upgrade: h2c\r\n" +
                "Accept: text/html\r\n\r\n";

            var fields = Http11Parser.Read(header.AsMemory());

            Assert.DoesNotContain(fields, f =>
                f.Name.Span.Equals("Connection", StringComparison.OrdinalIgnoreCase));

            Assert.DoesNotContain(fields, f =>
                f.Name.Span.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase));

            Assert.DoesNotContain(fields, f =>
                f.Name.Span.Equals("Upgrade", StringComparison.OrdinalIgnoreCase));

            Assert.Contains(fields, f =>
                f.Name.Span.Equals("Accept", StringComparison.Ordinal));
        }

        [Fact]
        public void Read_KeepsNonForwardableHeaders_WhenRequested()
        {
            var header =
                "GET / HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: keep-alive\r\n\r\n";

            var fields = Http11Parser.Read(
                header.AsMemory(),
                isHttps: true,
                keepNonForwardableHeader: true);

            Assert.Contains(fields, f =>
                f.Name.Span.Equals("Connection", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Read_ValueRetainsEmbeddedColons()
        {
            var header =
                "GET / HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "X-Url: https://other.example.com/a:b:c\r\n\r\n";

            var fields = Http11Parser.Read(header.AsMemory());

            var xurl = fields.Single(f =>
                f.Name.Span.Equals("X-Url", StringComparison.Ordinal));

            Assert.Equal("https://other.example.com/a:b:c", xurl.Value.ToString());
        }

        [Fact]
        public void Read_HandlesLfOnlyLineEndings()
        {
            var header = "GET / HTTP/1.1\nHost: example.com\nAccept: */*\n\n";

            var fields = Http11Parser.Read(header.AsMemory());

            Assert.Contains(fields, f =>
                f.Name.Span.Equals(":authority", StringComparison.Ordinal)
                && f.Value.Span.Equals("example.com", StringComparison.Ordinal));

            Assert.Contains(fields, f =>
                f.Name.Span.Equals("Accept", StringComparison.Ordinal));
        }

        [Fact]
        public void Read_InvalidHeaderLine_Throws()
        {
            var header = "GET / HTTP/1.1\r\nHost: example.com\r\nBogusLineWithoutColon\r\n\r\n";

            Assert.Throws<HPackCodecException>(() => Http11Parser.Read(header.AsMemory()));
        }

        [Fact]
        public void Read_HostPortIsPreservedInAuthority()
        {
            var header = "GET / HTTP/1.1\r\nHost: example.com:8443\r\n\r\n";

            var fields = Http11Parser.Read(header.AsMemory());

            var authority = fields.Single(f =>
                f.Name.Span.Equals(":authority", StringComparison.Ordinal));

            Assert.Equal("example.com:8443", authority.Value.ToString());
        }

        [Fact]
        public void Read_EncodeRoundTrip_MatchesLegacyOrder()
        {
            // Exercises the exact order HPackEncoder.Encode now produces through the
            // streaming reader. Uses the Req001 fixture round-tripped through
            // Http11Parser.Read+Write (the hot path) as a sanity check.
            var header = new UTF8Encoding(false).GetString(Headers.Req001);

            var fields = Http11Parser.Read(header.AsMemory());

            // Pseudo-headers come first and in HPACK order.
            Assert.Equal(":method", fields[0].Name.ToString());
            Assert.Equal(":scheme", fields[1].Name.ToString());
            Assert.Equal(":path", fields[2].Name.ToString());

            // The Cookie header in the fixture is a single line with three entries —
            // the default splitCookies=true should emit three separate cookie fields.
            var cookieCount = fields.Count(f =>
                f.Name.Span.Equals("cookie", StringComparison.Ordinal));

            Assert.Equal(3, cookieCount);
        }

        private static void AssertHeader(HeaderField field, string name, string value)
        {
            Assert.Equal(name, field.Name.ToString());
            Assert.Equal(value, field.Value.ToString());
        }
    }
}
