// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    /// <summary>
    ///     Regression tests for the header-received signal race: a body-less response
    ///     lets ProcessResponse skip the wait and dispose the stream before the read
    ///     loop signals. The former SemaphoreSlim(0, 1) signal then threw inside the
    ///     connection read loop, failing every in-flight exchange with a 528.
    /// </summary>
    public class StreamWorkerHeaderSignalTests
    {
        [Fact]
        public void HeadersDeliveredAfterDispose_DoesNotThrow()
        {
            using var fixture = new WorkerFixture();

            // Stream torn down before its response header block is delivered.
            fixture.Worker.Dispose();

            fixture.ReceiveResponseHeaders(endStream: true);
        }

        [Fact]
        public async Task HeadersDeliveryRacingProcessResponseAndDispose_NeverThrows()
        {
            // Production interleaving: the read loop sets the completed flag,
            // ProcessResponse skips the wait, completes and disposes, then the
            // read loop signals. A non-idempotent signal fails intermittently.
            for (var i = 0; i < 2000; i++) {
                using var fixture = new WorkerFixture();
                using var start = new ManualResetEventSlim();

                var deliver = Task.Run(() => {
                    start.Wait();
                    fixture.ReceiveResponseHeaders(endStream: true);
                });

                var consumeAndDispose = Task.Run(async () => {
                    start.Wait();
                    await fixture.Worker.ProcessResponse(CancellationToken.None, null!);
                    fixture.Worker.Dispose();
                });

                start.Set();
                await Task.WhenAll(deliver, consumeAndDispose);
            }
        }

        [Fact]
        public async Task HeadersDeliveredWhileProcessResponseWaits_Completes()
        {
            using var fixture = new WorkerFixture();

            var processResponse = Task.Run(
                () => fixture.Worker.ProcessResponse(CancellationToken.None, null!).AsTask());

            // Give the waiter a chance to park on the signal before delivery.
            await Task.Delay(50);

            fixture.ReceiveResponseHeaders(endStream: true);

            await processResponse;

            Assert.Same(Stream.Null, fixture.Exchange.Response.Body);
        }

        [Fact]
        public async Task ProcessResponseCancelledBeforeHeaders_SurfacesClientError()
        {
            using var fixture = new WorkerFixture();
            using var cts = new CancellationTokenSource();

            cts.Cancel();

            // Cancellation without GOAWAY must keep surfacing as ClientErrorException.
            await Assert.ThrowsAsync<ClientErrorException>(
                () => fixture.Worker.ProcessResponse(cts.Token, null!).AsTask());
        }

        private sealed class WorkerFixture : IDisposable
        {
            private readonly HPackEncoder _responseEncoder;
            private readonly StreamPool _streamPool;
            private readonly WindowSizeHolder _overallWindow;
            private readonly CancellationTokenSource _resetTokenSource;

            public WorkerFixture()
            {
                var authority = new Authority("test.local", 443, true);
                var setting = new H2StreamSetting();
                var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
                var requestEncoder = new HPackEncoder(new EncodingContext(memoryProvider));
                var responseDecoder = new HPackDecoder(new DecodingContext(authority, memoryProvider));
                var headerEncoder = new HeaderEncoder(requestEncoder, responseDecoder, setting);

                _responseEncoder = new HPackEncoder(new EncodingContext(memoryProvider));
                _overallWindow = new WindowSizeHolder(setting.OverallWindowSize, 0);
                _resetTokenSource = new CancellationTokenSource();

                var context = new StreamContext(
                    connectionId: 1,
                    authority: authority,
                    setting: setting,
                    headerEncoder: headerEncoder,
                    upStreamChannel: static (ref WriteTask _) => { },
                    overallWindowSizeHolder: _overallWindow);

                _streamPool = new StreamPool(context);
                Exchange = new Exchange(
                    IIdProvider.FromZero,
                    authority,
                    "GET / HTTP/2.0\r\nhost: test.local\r\n\r\n".AsMemory(),
                    "HTTP/2",
                    DateTime.UtcNow);
                Worker = new StreamWorker(1, _streamPool, Exchange, _resetTokenSource);
            }

            public Exchange Exchange { get; }

            public StreamWorker Worker { get; }

            public void ReceiveResponseHeaders(bool endStream)
            {
                var fields = new List<HeaderField> { new(":status", "200") };
                var buffer = new byte[1024];
                var encodedLength = _responseEncoder.EncodeFields(fields, buffer).Length;
                var flags = HeaderFlags.EndHeaders |
                            (endStream ? HeaderFlags.EndStream : HeaderFlags.None);
                var frame = new HeadersFrame(buffer.AsMemory(0, encodedLength), flags);

                Worker.ReceiveHeaderFragmentFromConnection(ref frame);
            }

            public void Dispose()
            {
                Worker.Dispose();
                _streamPool.Dispose();
                _overallWindow.Dispose();
                _resetTokenSource.Dispose();
                _responseEncoder.Dispose();
            }
        }
    }
}
