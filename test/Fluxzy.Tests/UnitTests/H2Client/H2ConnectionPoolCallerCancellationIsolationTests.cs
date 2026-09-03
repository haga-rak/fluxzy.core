// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    public class H2ConnectionPoolCallerCancellationIsolationTests
    {
        [Fact]
        public async Task CallerCancellationOfOneStream_DoesNotTearDownSiblingStreams()
        {
            using var pipe = new DuplexPipe();
            var baseStream = new RecomposedStream(pipe.ClientReadStream, pipe.ClientWriteStream);

            var authority = new Authority("test.local", 443, true);
            var setting = new H2StreamSetting();
            var connection = new Connection(authority, new TestIdProvider());

            var faulted = 0;

            await using var pool = new H2ConnectionPool(
                baseStream, setting, authority, connection,
                onConnectionFaulted: _ => Interlocked.Increment(ref faulted));

            pool.Init();

            var header = "GET / HTTP/2.0\r\nhost: test.local\r\n\r\n".AsMemory();
            var exchangeA = new Exchange(new TestIdProvider(), authority, header, "HTTP/2", DateTime.UtcNow);
            var exchangeB = new Exchange(new TestIdProvider(), authority, header, "HTTP/2", DateTime.UtcNow);

            using var bufferA = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(0x4000);
            using var bufferB = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(0x4000);
            using var ctsA = new CancellationTokenSource();
            using var ctsB = new CancellationTokenSource();

            var sendA = pool.Send(exchangeA, null!, bufferA, null!, ctsA.Token).AsTask();
            var sendB = pool.Send(exchangeB, null!, bufferB, null!, ctsB.Token).AsTask();

            await WaitForActiveStreamsAsync(pool, 2, sendA, sendB);

            ctsA.Cancel();

            await Assert.ThrowsAnyAsync<Exception>(() => WithTimeout(sendA));

            var completed = await Task.WhenAny(sendB, Task.Delay(500));

            Assert.True(completed != sendB,
                $"Sibling stream failed after a caller cancellation: {(sendB.IsFaulted ? sendB.Exception!.InnerException!.Message : sendB.Status.ToString())}");

            Assert.False(pool.Complete, "pool must stay usable after a per-stream caller cancellation");
            Assert.Equal(0, faulted);
            Assert.Equal(1, pool.StreamPoolForTests.ActiveStreamCount);

            var (rstStreamId, rstErrorCode) = await ReadRstStreamAsync(pipe.ServerReadStream);

            Assert.Equal(1, rstStreamId);
            Assert.Equal(H2ErrorCode.Cancel, rstErrorCode);

            ctsB.Cancel();
            await Assert.ThrowsAnyAsync<Exception>(() => WithTimeout(sendB));
        }

        private static async Task<(int StreamId, H2ErrorCode ErrorCode)> ReadRstStreamAsync(Stream serverReadStream)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var preface = new byte[H2Constants.Preface.Length];
            await serverReadStream.ReadExactlyAsync(preface, cts.Token);

            var header = new byte[9];

            while (true) {
                await serverReadStream.ReadExactlyAsync(header, cts.Token);

                var length = (header[0] << 16) | (header[1] << 8) | header[2];
                var type = (H2FrameType) header[3];
                var streamId = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(5)) & 0x7FFFFFFF;

                var payload = new byte[length];
                await serverReadStream.ReadExactlyAsync(payload, cts.Token);

                if (type == H2FrameType.RstStream)
                    return (streamId, (H2ErrorCode) BinaryPrimitives.ReadInt32BigEndian(payload));
            }
        }

        private static async Task WaitForActiveStreamsAsync(H2ConnectionPool pool, int count, params Task[] sendTasks)
        {
            var sw = Stopwatch.StartNew();

            while (pool.StreamPoolForTests.ActiveStreamCount < count) {
                foreach (var sendTask in sendTasks) {
                    if (sendTask.IsCompleted)
                        await sendTask;
                }

                if (sw.Elapsed > TimeSpan.FromSeconds(5))
                    throw new TimeoutException("Streams were never registered within 5s.");

                await Task.Delay(2);
            }
        }

        private static async Task WithTimeout(Task task)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, timeoutCts.Token));

            if (completed != task)
                throw new TimeoutException("Send did not complete within 5s after cancellation.");

            timeoutCts.Cancel();
            await task;
        }
    }
}
