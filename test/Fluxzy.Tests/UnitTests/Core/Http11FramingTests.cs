// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;
using Fluxzy;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    /// <summary>
    ///     Covers the HTTP/1.1 request/response framing normalization that closes the
    ///     smuggling vectors (RFC 7230 §3.3.3): Content-Length kept alongside
    ///     Transfer-Encoding: chunked, duplicate/conflicting Content-Length, and invalid
    ///     Content-Length values.
    /// </summary>
    public class Http11FramingTests
    {
        private static RequestHeader BuildRequest(params string[] headerLines)
        {
            var builder = new StringBuilder();

            builder.Append("POST /submit HTTP/1.1\r\n");
            builder.Append("Host: example.com\r\n");

            foreach (var line in headerLines) {
                builder.Append(line);
                builder.Append("\r\n");
            }

            builder.Append("\r\n");

            return new RequestHeader(builder.ToString().AsMemory(), false);
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            var count = 0;
            var index = 0;

            while ((index = haystack.IndexOf(needle, index, StringComparison.OrdinalIgnoreCase)) >= 0) {
                count++;
                index += needle.Length;
            }

            return count;
        }

        [Fact]
        public void ChunkedBody_Strips_Content_Length()
        {
            // Vector 2: Content-Length next to Transfer-Encoding: chunked. The proxy must
            // forward chunked only, otherwise an origin that trusts Content-Length desyncs.
            var header = BuildRequest("Content-Length: 8", "Transfer-Encoding: chunked");

            Assert.True(header.ChunkedBody);
            Assert.Equal(-1, header.ContentLength);

            var forwarded = header.GetHttp11Header().ToString();

            Assert.Equal(0, CountOccurrences(forwarded, "content-length"));
            Assert.Contains("transfer-encoding: chunked", forwarded, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ChunkedBody_Detected_When_It_Is_The_Final_Coding()
        {
            // "gzip, chunked" still means the body is chunked on the wire.
            var header = BuildRequest("Content-Length: 8", "Transfer-Encoding: gzip, chunked");

            Assert.True(header.ChunkedBody);
            Assert.Equal(-1, header.ContentLength);
            Assert.Equal(0, CountOccurrences(header.GetHttp11Header().ToString(), "content-length"));
        }

        [Fact]
        public void Duplicate_Equal_Content_Length_Collapses_To_One()
        {
            // Vector 1 (benign variant): equal duplicates are recoverable, but we must
            // never forward two Content-Length fields.
            var header = BuildRequest("Content-Length: 5", "Content-Length: 5");

            Assert.False(header.ChunkedBody);
            Assert.Equal(5, header.ContentLength);
            Assert.Equal(1, CountOccurrences(header.GetHttp11Header().ToString(), "content-length"));
        }

        [Fact]
        public void Duplicate_Conflicting_Content_Length_Is_Rejected()
        {
            // Vector 1: differing duplicates are an unrecoverable framing conflict.
            Assert.Throws<InvalidHttpFramingException>(
                () => BuildRequest("Content-Length: 5", "Content-Length: 6"));
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("+5")]
        [InlineData("abc")]
        [InlineData("5, 5")]
        [InlineData("0x10")]
        [InlineData("")]
        [InlineData("9223372036854775808")] // long.MaxValue + 1
        public void Invalid_Content_Length_Is_Rejected(string value)
        {
            // Vector 3: a value that is not 1*DIGIT cannot be framed; reject instead of
            // silently treating the body as empty and leaving the bytes on the socket.
            Assert.Throws<InvalidHttpFramingException>(
                () => BuildRequest($"Content-Length: {value}"));
        }

        [Theory]
        [InlineData("0", 0L)]
        [InlineData("42", 42L)]
        [InlineData("9223372036854775807", long.MaxValue)]
        public void Valid_Content_Length_Is_Preserved(string value, long expected)
        {
            var header = BuildRequest($"Content-Length: {value}");

            Assert.False(header.ChunkedBody);
            Assert.Equal(expected, header.ContentLength);
            Assert.Equal(1, CountOccurrences(header.GetHttp11Header().ToString(), "content-length"));
        }

        [Fact]
        public void Content_Length_Tolerates_Surrounding_Whitespace()
        {
            // OWS (HTAB) around the value is allowed and must not be mistaken for an
            // invalid value.
            var header = BuildRequest("Content-Length: \t42\t");

            Assert.Equal(42, header.ContentLength);
        }

        [Fact]
        public void ForceTransferChunked_Strips_Content_Length()
        {
            // The request-body substitution path calls ForceTransferChunked; without
            // dropping Content-Length it would emit CL + TE to the origin itself.
            var header = BuildRequest("Content-Length: 10");

            Assert.Equal(10, header.ContentLength);
            Assert.False(header.ChunkedBody);

            header.ForceTransferChunked();

            Assert.True(header.ChunkedBody);
            Assert.Equal(-1, header.ContentLength);

            var forwarded = header.GetHttp11Header().ToString();

            Assert.Equal(0, CountOccurrences(forwarded, "content-length"));
            Assert.Contains("transfer-encoding: chunked", forwarded, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Response_With_Conflicting_Content_Length_Is_Rejected()
        {
            // The same normalization protects the response path (response splitting).
            var raw = "HTTP/1.1 200 OK\r\nContent-Length: 3\r\nContent-Length: 4\r\n\r\n";

            Assert.Throws<InvalidHttpFramingException>(
                () => new ResponseHeader(raw.AsMemory(), false, true));
        }
    }
}
