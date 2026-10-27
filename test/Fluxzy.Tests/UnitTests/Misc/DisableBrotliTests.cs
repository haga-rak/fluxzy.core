// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using Fluxzy;
using Fluxzy.Core;
using Fluxzy.Extensions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    [Collection("DisableBrotli")]
    public class DisableBrotliTests : IDisposable
    {
        private readonly bool _initial = FluxzySharedSetting.DisableBrotli;

        public DisableBrotliTests()
        {
            FluxzySharedSetting.DisableBrotli = true;
        }

        public void Dispose()
        {
            FluxzySharedSetting.DisableBrotli = _initial;
        }

        [Theory]
        [InlineData("8.0.11", true)]
        [InlineData("8.0.32", true)]
        [InlineData("8.0.33", false)]
        [InlineData("9.0.21", true)]
        [InlineData("9.0.22", false)]
        [InlineData("10.0.10", true)]
        [InlineData("10.0.13", true)]
        [InlineData("10.0.14", false)]
        [InlineData("10.0.20", false)]
        [InlineData("12.0.0", false)]
        [InlineData("6.0.36", true)]
        public void Runtime_version_table(string version, bool vulnerable)
        {
            Assert.Equal(vulnerable, FluxzySharedSetting.IsBrotliRuntimeVulnerable(Version.Parse(version)));
        }

        [Theory]
        [InlineData("br")]
        [InlineData("brotli")]
        public void Brotli_decoding_is_refused(string token)
        {
            var ex = Assert.Throws<FluxzyException>(
                () => CompressionHelper.GetDecodedStream(token, new MemoryStream()));

            Assert.Contains("FLUXZY_DISABLE_BROTLI", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("gzip, deflate, br", "gzip, deflate")]
        [InlineData("gzip, deflate, br, zstd", "gzip, deflate, zstd")]
        [InlineData("br;q=1.0, gzip;q=0.8", "gzip;q=0.8")]
        [InlineData("BR", null)]
        [InlineData("gzip", "gzip")]
        public void Br_is_removed_from_accept_encoding(string original, string? expected)
        {
            var header = new RequestHeader(
                $"GET / HTTP/1.1\r\nHost: example.com\r\nAccept-Encoding: {original}\r\n\r\n".AsMemory(), true);

            header.RemoveAcceptEncodingToken("br");

            var value = header["accept-encoding"].Select(h => h.Value.ToString()).SingleOrDefault();

            Assert.Equal(expected, value);
        }
    }
}
