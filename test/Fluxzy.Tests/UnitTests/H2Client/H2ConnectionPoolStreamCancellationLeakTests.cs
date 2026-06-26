// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    /// <summary>
    ///     Reproducer for the post-registration permit leak of haga-rak/fluxzy.core#634:
    ///     when the caller cancels while <see cref="H2ConnectionPool.Send"/> is still setting
    ///     up a registered stream, the exchange used to unwind without disposing it — leaking
    ///     a barrier permit and pinning <see cref="StreamPool.ActiveStreamCount"/>. Drives the
    ///     real Send over a never-answering loopback and asserts a failed exchange leaves zero
    ///     active streams and all permits restored.
    /// </summary>
    public class H2ConnectionPoolStreamCancellationLeakTests
    {
        [Fact]
        public async Task CallerCancellationAfterRegistration_DoesNotLeakPermitOrPinPool()
        {
            const int maxConcurrent = 8;

            using var pipe = new DuplexPipe();
            var baseStream = new RecomposedStream(pipe.ClientReadStream, pipe.ClientWriteStream);

            var authority = new Authority("test.local", 443, true);
            var setting = new H2StreamSetting();
            setting.Remote.SettingsMaxConcurrentStreams = maxConcurrent;

            var connection = new Connection(authority, new TestIdProvider());

            await using var pool = new H2ConnectionPool(
                baseStream, setting, authority, connection,
                onConnectionFaulted: _ => { });

            pool.Init();

            Assert.Equal(maxConcurrent, AvailablePermits(pool));

            // No-body GET: once the header is sent the exchange parks in ProcessResponse
            // waiting for a response the silent server never sends.
            var header = "GET / HTTP/2.0\r\nhost: test.local\r\n\r\n".AsMemory();
            var exchange = new Exchange(new TestIdProvider(), authority, header, "HTTP/2", DateTime.UtcNow);

            using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(0x4000);
            using var callerCts = new CancellationTokenSource();

            var sendTask = pool.Send(exchange, null!, buffer, null!, callerCts.Token).AsTask();

            // Cancel only once the stream is registered — the window this leak lives in.
            await WaitForActiveStreamAsync(pool, sendTask);

            Assert.Equal(1, pool.StreamPoolForTests.ActiveStreamCount);
            Assert.Equal(maxConcurrent - 1, AvailablePermits(pool));

            callerCts.Cancel();

            // Fails with OCE (header-sent wait) or ClientErrorException (ProcessResponse) —
            // both are the leak's exit paths.
            await Assert.ThrowsAnyAsync<Exception>(() => WithTimeout(sendTask));

            // Invariant (fails pre-fix): a failed exchange leaves no active stream and
            // returns every permit it borrowed.
            Assert.Equal(0, pool.StreamPoolForTests.ActiveStreamCount);

            var leaked = maxConcurrent - AvailablePermits(pool);
            Assert.True(leaked == 0,
                $"Leaked {leaked} of {maxConcurrent} concurrency permits after a cancelled " +
                $"exchange while ActiveStreamCount={pool.StreamPoolForTests.ActiveStreamCount}.");
        }

        private static async Task WaitForActiveStreamAsync(H2ConnectionPool pool, Task sendTask)
        {
            var sw = Stopwatch.StartNew();

            while (pool.StreamPoolForTests.ActiveStreamCount == 0) {
                if (sendTask.IsCompleted)
                    // Surfaces the real exception if Send failed before registering a stream.
                    await sendTask;

                if (sw.Elapsed > TimeSpan.FromSeconds(5))
                    throw new TimeoutException("Stream was never registered within 5s.");

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
            await task; // observe / rethrow
        }

        private static int AvailablePermits(H2ConnectionPool pool)
        {
            var field = typeof(StreamPool).GetField(
                "_maxConcurrentStreamBarrier",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var semaphore = (SemaphoreSlim) field!.GetValue(pool.StreamPoolForTests)!;
            return semaphore.CurrentCount;
        }
    }
}
