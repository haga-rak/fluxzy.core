// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Rules.Actions;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class DuplicateTransferEncodingTests
    {
        /// <summary>
        ///     Reproduces https://github.com/haga-rak/fluxzy.core/issues/615:
        ///     when an upstream HTTP/1.1 server responds with
        ///     <c>Transfer-Encoding: chunked</c> and no <c>Content-Length</c>,
        ///     Fluxzy emits a duplicate <c>Transfer-Encoding: chunked</c> header on
        ///     the HTTP/1.1 downstream side. Strict HTTP clients reject it (Go's
        ///     <c>net/http</c> fails with
        ///     <c>too many transfer encodings: ["chunked" "chunked"]</c>).
        ///
        ///     The reproduction uses the same endpoint mentioned in the issue
        ///     (<c>https://noaa-goes16.s3.amazonaws.com/</c>) and pins the upstream
        ///     leg to HTTP/1.1 via <see cref="ForceHttp11Action"/> so the buggy
        ///     code path in <c>Header.ForceTransferChunked()</c> is exercised.
        /// </summary>
        [Fact]
        public async Task UpstreamChunkedResponse_DoesNotDuplicateTransferEncoding_Http11Downstream()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            // Force the fluxzy → origin leg to HTTP/1.1 so the upstream actually sends
            // Transfer-Encoding: chunked (in HTTP/2, chunking is implicit and the header
            // is forbidden, which hides the bug).
            setting.ConfigureRule()
                   .WhenAny()
                   .Do(new ForceHttp11Action());

            await using var proxy = new Proxy(setting);

            var capture = new TaskCompletionSource<Exchange>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            proxy.Writer.ExchangeUpdated += (_, args) =>
            {
                // AfterResponseHeader fires after the orchestrator has finalised the
                // header set that will be written downstream — i.e. after the call
                // to ForceTransferChunked() that introduces the duplication.
                if (args.UpdateType == ArchiveUpdateType.AfterResponseHeader)
                    capture.TrySetResult(args.Original);
            };

            var endPoints = proxy.Run();

            using var client = HttpClientUtility.CreateHttpClient(
                endPoints,
                setting,
                handler => handler.ServerCertificateCustomValidationCallback =
                    (_, _, _, _) => true);
            client.Timeout = TimeSpan.FromSeconds(30);

            var response = await client.GetAsync("https://noaa-goes16.s3.amazonaws.com/");

            Assert.True(response.IsSuccessStatusCode,
                $"Expected 2xx from noaa-goes16.s3.amazonaws.com, got {(int)response.StatusCode}");

            var exchange = await capture.Task.WaitAsync(TimeSpan.FromSeconds(30));

            // Sanity: the upstream response must actually be chunked, otherwise this
            // test would not exercise the buggy code path.
            Assert.True(exchange.Response.Header!.ChunkedBody,
                "Upstream response must use Transfer-Encoding: chunked to reproduce issue #615");

            // The bug: Header.ForceTransferChunked() unconditionally appends another
            // Transfer-Encoding: chunked header, even when one was already present on
            // the upstream response. This assertion fails while the bug is live.
            var transferEncodings = exchange.Response.Header.HeaderFields
                .Where(h => h.Name.Span.Equals(
                    "Transfer-Encoding".AsSpan(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.True(transferEncodings.Count == 1,
                $"Expected exactly one Transfer-Encoding header, got {transferEncodings.Count}. " +
                "Issue #615: Header.ForceTransferChunked() appends a duplicate when the " +
                "upstream response already carries Transfer-Encoding: chunked.");
        }
    }
}
