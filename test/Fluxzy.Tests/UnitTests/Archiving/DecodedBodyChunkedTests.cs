// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Extensions;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Writers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Archiving
{
    /// <summary>
    ///     A response without content-length ends up with Transfer-Encoding: chunked in the
    ///     archived headers, either sent by an HTTP/1.1 upstream or added by the proxy for
    ///     an HTTP/1.1 client in front of an HTTP/2 upstream. The stored body is never chunked,
    ///     so the decoded body reader must only undo the content-encoding.
    /// </summary>
    public class DecodedBodyChunkedTests
    {
        private static readonly string Payload = string.Concat(Enumerable.Repeat("hello chunked gzip world ", 40));

        [Theory]
        [InlineData(HttpProtocols.Http1)]
        [InlineData(HttpProtocols.Http2)]
        public async Task GetDecodedResponseBody_Gzip_Without_Content_Length(HttpProtocols upstreamProtocols)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"fluxzy-decoded-{Guid.NewGuid():N}");

            try {
                await using var host = await InProcessHost.Create(app => app.MapGet("/gzip", async context => {
                    context.Response.Headers.ContentEncoding = "gzip";
                    context.Response.ContentType = "text/plain";

                    await using var gzip = new GZipStream(context.Response.Body, CompressionLevel.Fastest, true);
                    await gzip.WriteAsync(Encoding.UTF8.GetBytes(Payload));
                }), protocols: upstreamProtocols);

                var setting = FluxzySetting.CreateLocalRandomPort();
                setting.SetArchivingPolicy(ArchivingPolicy.CreateFromDirectory(directory));
                setting.AddAlterationRules(new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

                var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                await using var proxy = new Proxy(setting);

                proxy.Writer.ExchangeUpdated += (_, args) => {
                    if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                        completed.TrySetResult();
                };

                var endPoint = proxy.Run().First();

                using var client = Socks5ClientFactory.Create(endPoint, httpVersion: new Version(1, 1));
                client.BaseAddress = new Uri(host.BaseUrl);

                await using (var wire = await client.GetStreamAsync("/gzip"))
                await using (var clientGzip = new GZipStream(wire, CompressionMode.Decompress)) {
                    Assert.Equal(Payload, await clientGzip.ReadToEndGreedyAsync());
                }

                await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

                using var reader = new DirectoryArchiveReader(directory);
                var exchange = Assert.Single(reader.ReadAllExchanges());

                // The scenario is only meaningful if chunked shows up in the recorded headers
                Assert.True(exchange.IsResponseChunkedTransferEncoded(true));
                Assert.Equal("gzip", exchange.GetResponseContentEncoding());

                using var decoded = reader.GetDecodedResponseBody(exchange.Id)!;

                Assert.Equal(Payload, decoded.ReadToEndGreedy());
            }
            finally {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }
    }
}
