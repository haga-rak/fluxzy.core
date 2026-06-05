// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli;
using Fluxzy.Cli.Commands;
using Fluxzy.Extensions;
using Fluxzy.Misc.Streams;
using Xunit;
using ZstdSharp;

namespace Fluxzy.Tests.UnitTests.Misc
{
    /// <summary>
    ///     The CLI plugs a zstd decoder into <see cref="ContentDecoderRegistry" /> at startup (Fluxzy.Core
    ///     ships none by design). These tests check the registration happens and that the body-decode path
    ///     used by injection/substitution now handles a <c>Content-Encoding: zstd</c> body — closing the gap
    ///     described in <see cref="InjectHtmlTagZstdDiagnosisTests" />.
    /// </summary>
    public class ZstdContentDecoderCliTests
    {
        private const string Html =
            "<html lang=\"fr\">\n<head><script>var x = 1;</script></head>\n<body>hello</body></html>";

        [Fact]
        public async Task Run_registers_zstd_decoder()
        {
            await FluxzyStartup.Run(new[] { "--version" }, OutputConsole.CreateEmpty(), CancellationToken.None);

            Assert.True(ContentDecoderRegistry.Contains("zstd"));
        }

        [Fact]
        public async Task Registered_decoder_round_trips()
        {
            await FluxzyStartup.Run(new[] { "--version" }, OutputConsole.CreateEmpty(), CancellationToken.None);

            var raw = Encoding.UTF8.GetBytes(Html);

            using var decoded = CompressionHelper.GetDecodedStream("zstd", new MemoryStream(Zstd(raw)));
            using var output = new MemoryStream();
            decoded.CopyTo(output);

            Assert.Equal(Html, Encoding.UTF8.GetString(output.ToArray()));
        }

        [Fact]
        public async Task Injection_succeeds_on_zstd_encoded_body()
        {
            await FluxzyStartup.Run(new[] { "--version" }, OutputConsole.CreateEmpty(), CancellationToken.None);

            var compressed = Zstd(Encoding.UTF8.GetBytes(Html));

            // The substitution path decodes the body first, then scans for <head>.
            using var decoded = CompressionHelper.GetDecodedStream("zstd", new MemoryStream(compressed));
            var injected = Inject(decoded);

            Assert.Contains("<head><!--INJECTED-->", injected, StringComparison.Ordinal);
        }

        private static string Inject(Stream body)
        {
            var matcher = new SimpleHtmlTagOpeningMatcher(Encoding.UTF8, StringComparison.OrdinalIgnoreCase, false);

            using var stream = new InjectStreamOnStream(
                body,
                matcher,
                Encoding.UTF8.GetBytes("head"),
                new MemoryStream(Encoding.UTF8.GetBytes("<!--INJECTED-->")));

            using var output = new MemoryStream();
            stream.CopyTo(output);

            return Encoding.UTF8.GetString(output.ToArray());
        }

        private static byte[] Zstd(byte[] data)
        {
            using var memory = new MemoryStream();

            using (var zstd = new CompressionStream(memory, leaveOpen: true)) {
                zstd.Write(data, 0, data.Length);
            }

            return memory.ToArray();
        }
    }
}
