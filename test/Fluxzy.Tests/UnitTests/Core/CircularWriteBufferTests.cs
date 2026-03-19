// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class CircularWriteBufferTests
    {
        /// <summary>
        ///     Wraps a CircularWriteBuffer with a SemaphoreSlim-based wait mechanism,
        ///     replicating the old WaitForDataAsync behavior for test consumer loops.
        /// </summary>
        private sealed class TestableBuffer : IDisposable
        {
            public readonly CircularWriteBuffer Buffer;
            private readonly SemaphoreSlim _signal = new(0);

            public TestableBuffer(int capacity)
            {
                Buffer = new CircularWriteBuffer(capacity, () => {
                    try { _signal.Release(); } catch (ObjectDisposedException) { }
                });
            }

            public async ValueTask<bool> WaitForDataAsync(CancellationToken ct)
            {
                while (true) {
                    if (Buffer.HasData) return true;
                    if (Buffer.IsCompleted) return false;
                    await _signal.WaitAsync(ct).ConfigureAwait(false);
                }
            }

            public void Dispose()
            {
                Buffer.Dispose();
                _signal.Dispose();
            }
        }

        private static long HashBytes(ReadOnlyMemory<byte> mem, long hash)
        {
            foreach (var b in mem.ToArray())
                hash = hash * 31 + b;

            return hash;
        }

        // ──────────────────────────────────────────────
        // Construction
        // ──────────────────────────────────────────────

        [Fact]
        public void Ctor_ZeroCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircularWriteBuffer(0));
        }

        [Fact]
        public void Ctor_NegativeCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircularWriteBuffer(-1));
        }

        // ──────────────────────────────────────────────
        // Basic write / read roundtrip
        // ──────────────────────────────────────────────

        [Fact]
        public void Write_Then_Read_SingleSegment()
        {
            using var buf = new CircularWriteBuffer(1024);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            buf.Write(data);

            Assert.True(buf.HasData);

            buf.GetReadableRegions(out var seg1, out var seg2, out var total);

            Assert.Equal(5, total);
            Assert.Equal(data, seg1.ToArray());
            Assert.True(seg2.IsEmpty);

            buf.Advance(total);

            // Buffer should be empty now
            buf.GetReadableRegions(out _, out _, out var afterTotal);
            Assert.Equal(0, afterTotal);
        }

        [Fact]
        public void Write_Empty_Span_Is_Noop()
        {
            using var buf = new CircularWriteBuffer(64);

            buf.Write(ReadOnlySpan<byte>.Empty);
            buf.Complete();

            // No data was written
            Assert.False(buf.HasData);
            Assert.True(buf.IsCompleted);
        }

        [Fact]
        public void Write_Exceeding_Capacity_Throws()
        {
            using var buf = new CircularWriteBuffer(16);
            var data = new byte[17];

            Assert.Throws<InvalidOperationException>(() => buf.Write(data));
        }

        // ──────────────────────────────────────────────
        // Multiple writes coalesce before read
        // ──────────────────────────────────────────────

        [Fact]
        public void Multiple_Writes_Coalesce_Into_Single_Read()
        {
            using var buf = new CircularWriteBuffer(1024);

            buf.Write(new byte[] { 0xAA, 0xBB });
            buf.Write(new byte[] { 0xCC, 0xDD, 0xEE });

            Assert.True(buf.HasData);

            buf.GetReadableRegions(out var seg1, out var seg2, out var total);

            Assert.Equal(5, total);
            Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE }, seg1.ToArray());
            Assert.True(seg2.IsEmpty);
        }

        // ──────────────────────────────────────────────
        // Fill buffer exactly to capacity
        // ──────────────────────────────────────────────

        [Fact]
        public void Fill_Exactly_To_Capacity()
        {
            const int cap = 64;
            using var buf = new CircularWriteBuffer(cap);

            var data = Enumerable.Range(0, cap).Select(i => (byte)i).ToArray();
            buf.Write(data);

            Assert.True(buf.HasData);

            buf.GetReadableRegions(out var seg1, out var seg2, out var total);

            Assert.Equal(cap, total);
            Assert.Equal(data, seg1.ToArray());
            Assert.True(seg2.IsEmpty);
        }

        // ──────────────────────────────────────────────
        // Wrap-around produces two segments
        // ──────────────────────────────────────────────

        [Fact]
        public void Wraparound_Produces_Two_Segments()
        {
            const int cap = 16;
            using var buf = new CircularWriteBuffer(cap);

            // Fill first 12 bytes
            var first = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            buf.Write(first);

            // Consume 10 bytes — tail moves to 10, head stays at 12
            Assert.True(buf.HasData);
            buf.GetReadableRegions(out _, out _, out var total1);
            Assert.Equal(12, total1);
            buf.Advance(10);

            // Now: tail=10, head=12, count=2. Free space = 14.
            // Write 8 bytes — should wrap: 4 bytes at end [12..16), 4 bytes at start [0..4)
            var second = new byte[] { 20, 21, 22, 23, 24, 25, 26, 27 };
            buf.Write(second);

            Assert.True(buf.HasData);

            buf.GetReadableRegions(out var seg1, out var seg2, out var total2);

            Assert.Equal(10, total2); // 2 remaining + 8 new

            // seg1: from tail=10 to end=16 → 6 bytes
            Assert.Equal(6, seg1.Length);
            Assert.Equal(new byte[] { 11, 12, 20, 21, 22, 23 }, seg1.ToArray());

            // seg2: from 0 to head=4 → 4 bytes
            Assert.Equal(4, seg2.Length);
            Assert.Equal(new byte[] { 24, 25, 26, 27 }, seg2.ToArray());
        }

        // ──────────────────────────────────────────────
        // Write that lands exactly at the end (head resets to 0)
        // ──────────────────────────────────────────────

        [Fact]
        public void Write_Exactly_To_End_Resets_Head()
        {
            const int cap = 8;
            using var buf = new CircularWriteBuffer(cap);

            // Fill exactly to end
            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            buf.Write(data);

            Assert.True(buf.HasData);
            buf.GetReadableRegions(out var seg1, out _, out var total);
            Assert.Equal(8, total);
            Assert.Equal(data, seg1.ToArray());

            // Drain fully
            buf.Advance(8);

            // Write again — should start from 0
            var data2 = new byte[] { 10, 20, 30 };
            buf.Write(data2);

            Assert.True(buf.HasData);
            buf.GetReadableRegions(out seg1, out var seg2, out total);
            Assert.Equal(3, total);
            Assert.Equal(data2, seg1.ToArray());
            Assert.True(seg2.IsEmpty);
        }

        // ──────────────────────────────────────────────
        // Complete / Dispose
        // ──────────────────────────────────────────────

        [Fact]
        public void Complete_Empty_Buffer_HasData_False()
        {
            using var buf = new CircularWriteBuffer(64);

            buf.Complete();

            Assert.False(buf.HasData);
            Assert.True(buf.IsCompleted);
        }

        [Fact]
        public void Complete_With_Pending_Data_HasData_Then_Empty()
        {
            using var buf = new CircularWriteBuffer(64);

            buf.Write(new byte[] { 1, 2, 3 });
            buf.Complete();

            // Data available
            Assert.True(buf.HasData);

            buf.GetReadableRegions(out var seg1, out _, out var total);
            Assert.Equal(3, total);
            buf.Advance(total);

            // Completed and empty
            Assert.False(buf.HasData);
            Assert.True(buf.IsCompleted);
        }

        [Fact]
        public void Write_After_Complete_Is_Silently_Ignored()
        {
            using var buf = new CircularWriteBuffer(64);

            buf.Complete();
            buf.Write(new byte[] { 1, 2, 3 }); // should not throw

            buf.GetReadableRegions(out _, out _, out var total);
            Assert.Equal(0, total);
        }

        [Fact]
        public void Dispose_Is_Idempotent()
        {
            var buf = new CircularWriteBuffer(64);
            buf.Dispose();
            buf.Dispose(); // should not throw
        }

        // ──────────────────────────────────────────────
        // HasData and IsCompleted properties
        // ──────────────────────────────────────────────

        [Fact]
        public void HasData_False_On_Empty_Buffer()
        {
            using var buf = new CircularWriteBuffer(64);
            Assert.False(buf.HasData);
        }

        [Fact]
        public void HasData_True_After_Write()
        {
            using var buf = new CircularWriteBuffer(64);
            buf.Write(new byte[] { 1 });
            Assert.True(buf.HasData);
        }

        [Fact]
        public void IsCompleted_False_Initially()
        {
            using var buf = new CircularWriteBuffer(64);
            Assert.False(buf.IsCompleted);
        }

        [Fact]
        public void IsCompleted_True_After_Complete()
        {
            using var buf = new CircularWriteBuffer(64);
            buf.Complete();
            Assert.True(buf.IsCompleted);
        }

        // ──────────────────────────────────────────────
        // OnWrite callback
        // ──────────────────────────────────────────────

        [Fact]
        public void OnWrite_Callback_Invoked_On_Write()
        {
            var callCount = 0;
            using var buf = new CircularWriteBuffer(64, () => callCount++);

            buf.Write(new byte[] { 1, 2 });
            Assert.Equal(1, callCount);

            buf.Write(new byte[] { 3 });
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void OnWrite_Callback_Invoked_On_Complete()
        {
            var callCount = 0;
            using var buf = new CircularWriteBuffer(64, () => callCount++);

            buf.Complete();
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void OnWrite_Callback_Not_Invoked_For_Empty_Write()
        {
            var callCount = 0;
            using var buf = new CircularWriteBuffer(64, () => callCount++);

            buf.Write(ReadOnlySpan<byte>.Empty);
            Assert.Equal(0, callCount);
        }

        // ──────────────────────────────────────────────
        // Back-pressure: producer blocks when buffer full
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Producer_Blocks_When_Buffer_Full()
        {
            const int cap = 16;
            using var buf = new CircularWriteBuffer(cap);

            // Fill completely
            buf.Write(new byte[cap]);

            var producerStarted = new ManualResetEventSlim(false);
            var producerDone = false;

            var producerTask = Task.Run(() =>
            {
                producerStarted.Set();
                buf.Write(new byte[] { 0xFF }); // should block
                producerDone = true;
            });

            producerStarted.Wait();
            await Task.Delay(100);

            // Producer should still be blocked
            Assert.False(producerDone);

            // Consumer drains some data to unblock producer
            Assert.True(buf.HasData);
            buf.GetReadableRegions(out _, out _, out var total);
            buf.Advance(total);

            await producerTask.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(producerDone);
        }

        [Fact]
        public async Task Complete_Unblocks_Waiting_Producer()
        {
            const int cap = 8;
            using var buf = new CircularWriteBuffer(cap);

            // Fill completely
            buf.Write(new byte[cap]);

            var producerTask = Task.Run(() =>
            {
                buf.Write(new byte[] { 0xFF }); // blocks
            });

            await Task.Delay(50);
            Assert.False(producerTask.IsCompleted);

            buf.Complete(); // should unblock the producer

            await producerTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        // ──────────────────────────────────────────────
        // Async consumer wakeup
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Consumer_Wakes_When_Producer_Writes()
        {
            using var tb = new TestableBuffer(1024);

            var waitTask = tb.WaitForDataAsync(CancellationToken.None).AsTask();

            await Task.Delay(50);
            Assert.False(waitTask.IsCompleted);

            tb.Buffer.Write(new byte[] { 42 });

            var result = await waitTask.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(result);
        }

        // ──────────────────────────────────────────────
        // Multiple produce-consume cycles
        // ──────────────────────────────────────────────

        [Fact]
        public void Many_ProduceConsume_Cycles_Preserve_Data_Integrity()
        {
            const int cap = 64;
            const int iterations = 500;
            using var buf = new CircularWriteBuffer(cap);

            for (var i = 0; i < iterations; i++) {
                var data = new byte[] { (byte)(i & 0xFF), (byte)((i >> 8) & 0xFF), (byte)(i * 7 & 0xFF) };

                buf.Write(data);

                Assert.True(buf.HasData);
                buf.GetReadableRegions(out var seg1, out var seg2, out var total);
                Assert.Equal(3, total);

                var read = new byte[total];
                seg1.CopyTo(read);

                if (!seg2.IsEmpty)
                    seg2.CopyTo(read.AsMemory(seg1.Length));

                Assert.Equal(data, read);

                buf.Advance(total);
            }
        }

        // ──────────────────────────────────────────────
        // Concurrent MPSC stress test
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Concurrent_MultiProducer_SingleConsumer_DataIntegrity()
        {
            const int cap = 4096;
            const int producerCount = 8;
            const int messagesPerProducer = 200;
            const int messageSize = 8; // 1 byte tag + 3 byte producer ID + 4 byte seq
            using var tb = new TestableBuffer(cap);

            var allReceived = new List<byte[]>();

            // Consumer task
            var consumerTask = Task.Run(async () =>
            {
                while (await tb.WaitForDataAsync(CancellationToken.None)) {
                    tb.Buffer.GetReadableRegions(out var seg1, out var seg2, out var total);

                    var flat = new byte[total];
                    seg1.CopyTo(flat);

                    if (!seg2.IsEmpty)
                        seg2.CopyTo(flat.AsMemory(seg1.Length));

                    tb.Buffer.Advance(total);

                    // Parse individual messages from the flat buffer
                    var offset = 0;

                    while (offset + messageSize <= flat.Length) {
                        var msg = new byte[messageSize];
                        Array.Copy(flat, offset, msg, 0, messageSize);
                        lock (allReceived) {
                            allReceived.Add(msg);
                        }

                        offset += messageSize;
                    }

                    // Shouldn't have leftover partial messages since each Write is exactly messageSize
                    Assert.Equal(flat.Length, offset);
                }
            });

            // Producer tasks
            var producerTasks = Enumerable.Range(0, producerCount).Select(pid => Task.Run(() =>
            {
                for (var seq = 0; seq < messagesPerProducer; seq++) {
                    var msg = new byte[messageSize];
                    msg[0] = 0xFE; // tag
                    msg[1] = (byte)((pid >> 0) & 0xFF);
                    msg[2] = (byte)((pid >> 8) & 0xFF);
                    msg[3] = (byte)((pid >> 16) & 0xFF);
                    msg[4] = (byte)((seq >> 0) & 0xFF);
                    msg[5] = (byte)((seq >> 8) & 0xFF);
                    msg[6] = (byte)((seq >> 16) & 0xFF);
                    msg[7] = (byte)((seq >> 24) & 0xFF);
                    tb.Buffer.Write(msg);
                }
            })).ToArray();

            await Task.WhenAll(producerTasks);
            tb.Buffer.Complete();
            await consumerTask.WaitAsync(TimeSpan.FromSeconds(10));

            // Verify: every message from every producer was received
            Assert.Equal(producerCount * messagesPerProducer, allReceived.Count);

            // Group by producer and verify all sequence numbers present
            var byProducer = allReceived.GroupBy(m => m[1] | (m[2] << 8) | (m[3] << 16))
                                        .ToDictionary(g => g.Key, g => g.ToList());

            Assert.Equal(producerCount, byProducer.Count);

            foreach (var (pid, messages) in byProducer) {
                Assert.Equal(messagesPerProducer, messages.Count);

                var seqs = messages.Select(m => m[4] | (m[5] << 8) | (m[6] << 16) | (m[7] << 24))
                                   .OrderBy(s => s)
                                   .ToArray();

                var expected = Enumerable.Range(0, messagesPerProducer).ToArray();
                Assert.Equal(expected, seqs);

                // All tags should be 0xFE — no corruption
                Assert.All(messages, m => Assert.Equal(0xFE, m[0]));
            }
        }

        // ──────────────────────────────────────────────
        // Concurrent stress with variable-size messages
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Concurrent_VariableSize_Messages_Integrity()
        {
            const int cap = 8192;
            const int producerCount = 4;
            const int messagesPerProducer = 300;
            using var tb = new TestableBuffer(cap);

            // Each message: [2-byte length][1-byte pid][payload...]
            // The consumer reconstructs messages by reading length-prefixed frames.
            var allReceived = new List<(byte pid, byte[] payload)>();

            // Consumer
            var consumerTask = Task.Run(async () =>
            {
                var reassembly = new byte[cap];
                var reassemblyLen = 0;

                while (await tb.WaitForDataAsync(CancellationToken.None)) {
                    tb.Buffer.GetReadableRegions(out var seg1, out var seg2, out var total);

                    // Append to reassembly buffer
                    seg1.CopyTo(reassembly.AsMemory(reassemblyLen));
                    reassemblyLen += seg1.Length;

                    if (!seg2.IsEmpty) {
                        seg2.CopyTo(reassembly.AsMemory(reassemblyLen));
                        reassemblyLen += seg2.Length;
                    }

                    tb.Buffer.Advance(total);

                    // Parse complete messages
                    var pos = 0;

                    while (pos + 3 <= reassemblyLen) {
                        var msgLen = reassembly[pos] | (reassembly[pos + 1] << 8);

                        if (pos + 2 + msgLen > reassemblyLen)
                            break; // incomplete message

                        var pid = reassembly[pos + 2];
                        var payload = new byte[msgLen - 1]; // exclude pid byte
                        Array.Copy(reassembly, pos + 3, payload, 0, payload.Length);

                        lock (allReceived) {
                            allReceived.Add((pid, payload));
                        }

                        pos += 2 + msgLen;
                    }

                    // Compact remaining data
                    if (pos > 0) {
                        Array.Copy(reassembly, pos, reassembly, 0, reassemblyLen - pos);
                        reassemblyLen -= pos;
                    }
                }

                Assert.Equal(0, reassemblyLen); // no leftover partial data
            });

            // Producers write variable-size messages
            var rng = new Random(42);
            var producerTasks = Enumerable.Range(0, producerCount).Select(pid => Task.Run(() =>
            {
                var localRng = new Random(42 + pid); // deterministic per producer

                for (var seq = 0; seq < messagesPerProducer; seq++) {
                    // Payload: fill with (pid ^ seq) pattern so we can verify
                    var payloadSize = localRng.Next(1, 60);
                    var frame = new byte[2 + 1 + payloadSize]; // length + pid + payload

                    var msgLen = 1 + payloadSize;
                    frame[0] = (byte)(msgLen & 0xFF);
                    frame[1] = (byte)((msgLen >> 8) & 0xFF);
                    frame[2] = (byte)pid;

                    var pattern = (byte)((pid * 37 + seq * 13) & 0xFF);

                    for (var i = 0; i < payloadSize; i++)
                        frame[3 + i] = pattern;

                    tb.Buffer.Write(frame);
                }
            })).ToArray();

            await Task.WhenAll(producerTasks);
            tb.Buffer.Complete();
            await consumerTask.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(producerCount * messagesPerProducer, allReceived.Count);

            // Verify per-producer message count
            var grouped = allReceived.GroupBy(m => m.pid).ToDictionary(g => g.Key, g => g.ToList());
            Assert.Equal(producerCount, grouped.Count);

            foreach (var (pid, msgs) in grouped) {
                Assert.Equal(messagesPerProducer, msgs.Count);
            }
        }

        // ──────────────────────────────────────────────
        // Tight capacity stress: buffer barely larger than message
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Tight_Buffer_Forces_Frequent_Wraps_And_Backpressure()
        {
            // Buffer only slightly larger than the message — every write wraps or blocks
            const int msgSize = 13;
            const int cap = msgSize * 2 + 1; // 27 bytes
            const int totalMessages = 1000;
            using var tb = new TestableBuffer(cap);

            var received = new List<byte[]>();

            var consumerTask = Task.Run(async () =>
            {
                while (await tb.WaitForDataAsync(CancellationToken.None)) {
                    tb.Buffer.GetReadableRegions(out var seg1, out var seg2, out var total);

                    var flat = new byte[total];
                    seg1.CopyTo(flat);

                    if (!seg2.IsEmpty)
                        seg2.CopyTo(flat.AsMemory(seg1.Length));

                    tb.Buffer.Advance(total);

                    var offset = 0;

                    while (offset + msgSize <= flat.Length) {
                        var msg = new byte[msgSize];
                        Array.Copy(flat, offset, msg, 0, msgSize);
                        received.Add(msg);
                        offset += msgSize;
                    }

                    // No partial messages
                    Assert.Equal(flat.Length, offset);
                }
            });

            var producerTask = Task.Run(() =>
            {
                for (var i = 0; i < totalMessages; i++) {
                    var msg = new byte[msgSize];

                    for (var j = 0; j < msgSize; j++)
                        msg[j] = (byte)((i + j) & 0xFF);

                    tb.Buffer.Write(msg);
                }
            });

            await producerTask;
            tb.Buffer.Complete();
            await consumerTask.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(totalMessages, received.Count);

            // Verify content
            for (var i = 0; i < totalMessages; i++) {
                for (var j = 0; j < msgSize; j++) {
                    Assert.Equal((byte)((i + j) & 0xFF), received[i][j]);
                }
            }
        }

        // ──────────────────────────────────────────────
        // GetReadableRegions when empty returns zeroes
        // ──────────────────────────────────────────────

        [Fact]
        public void GetReadableRegions_Empty_Buffer()
        {
            using var buf = new CircularWriteBuffer(64);

            buf.GetReadableRegions(out var seg1, out var seg2, out var total);

            Assert.Equal(0, total);
            Assert.True(seg1.IsEmpty);
            Assert.True(seg2.IsEmpty);
        }

        // ──────────────────────────────────────────────
        // Advance then re-read shows empty
        // ──────────────────────────────────────────────

        [Fact]
        public void Advance_Full_Amount_Empties_Buffer()
        {
            using var buf = new CircularWriteBuffer(64);
            buf.Write(new byte[] { 1, 2, 3, 4 });

            Assert.True(buf.HasData);
            buf.GetReadableRegions(out _, out _, out var total);
            buf.Advance(total);

            buf.GetReadableRegions(out var seg1, out var seg2, out var afterTotal);
            Assert.Equal(0, afterTotal);
            Assert.True(seg1.IsEmpty);
            Assert.True(seg2.IsEmpty);
        }

        // ──────────────────────────────────────────────
        // Partial advance
        // ──────────────────────────────────────────────

        [Fact]
        public void Partial_Advance_Leaves_Remaining_Data()
        {
            using var buf = new CircularWriteBuffer(64);
            buf.Write(new byte[] { 10, 20, 30, 40, 50 });

            Assert.True(buf.HasData);

            // Advance only 2 bytes
            buf.Advance(2);

            buf.GetReadableRegions(out var seg1, out _, out var total);
            Assert.Equal(3, total);
            Assert.Equal(new byte[] { 30, 40, 50 }, seg1.ToArray());
        }

        // ──────────────────────────────────────────────
        // HasData returns immediately when data present
        // ──────────────────────────────────────────────

        [Fact]
        public void HasData_Returns_True_When_Data_Present()
        {
            using var buf = new CircularWriteBuffer(64);
            buf.Write(new byte[] { 1 });

            Assert.True(buf.HasData);
        }

        // ──────────────────────────────────────────────
        // Consumer reads are correct after many wraps
        // ──────────────────────────────────────────────

        [Theory]
        [InlineData(7, 3, 500)]   // small buffer, small writes, many cycles
        [InlineData(32, 15, 200)] // write nearly half the buffer each time
        [InlineData(100, 99, 50)] // write almost full buffer each time
        public void Repeated_Wraps_Data_Integrity(int capacity, int writeSize, int iterations)
        {
            using var buf = new CircularWriteBuffer(capacity);

            for (var i = 0; i < iterations; i++) {
                var data = new byte[writeSize];

                for (var j = 0; j < writeSize; j++)
                    data[j] = (byte)((i * 7 + j * 3) & 0xFF);

                buf.Write(data);

                Assert.True(buf.HasData);
                buf.GetReadableRegions(out var seg1, out var seg2, out var total);
                Assert.Equal(writeSize, total);

                var result = new byte[total];
                seg1.CopyTo(result);

                if (!seg2.IsEmpty)
                    seg2.CopyTo(result.AsMemory(seg1.Length));

                Assert.Equal(data, result);

                buf.Advance(total);
            }
        }

        // ──────────────────────────────────────────────
        // Throughput: large continuous stream of data
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Large_Data_Transfer_Integrity()
        {
            const int cap = 16384;
            const int totalBytes = 1_000_000;
            const int chunkSize = 997; // prime, forces irregular wrap boundaries
            using var tb = new TestableBuffer(cap);

            var sentHash = 0L;
            var receivedHash = 0L;
            var totalReceived = 0;

            var consumerTask = Task.Run(async () =>
            {
                while (await tb.WaitForDataAsync(CancellationToken.None)) {
                    tb.Buffer.GetReadableRegions(out var seg1, out var seg2, out var total);

                    receivedHash = HashBytes(seg1, receivedHash);

                    if (!seg2.IsEmpty)
                        receivedHash = HashBytes(seg2, receivedHash);

                    totalReceived += total;
                    tb.Buffer.Advance(total);
                }
            });

            var producerTask = Task.Run(() =>
            {
                var sent = 0;
                var seqByte = (byte)0;

                while (sent < totalBytes) {
                    var toSend = Math.Min(chunkSize, totalBytes - sent);
                    var chunk = new byte[toSend];

                    for (var i = 0; i < toSend; i++) {
                        chunk[i] = seqByte;
                        sentHash = sentHash * 31 + seqByte;
                        seqByte = (byte)((seqByte + 1) & 0xFF);
                    }

                    tb.Buffer.Write(chunk);
                    sent += toSend;
                }
            });

            await producerTask;
            tb.Buffer.Complete();
            await consumerTask.WaitAsync(TimeSpan.FromSeconds(30));

            Assert.Equal(totalBytes, totalReceived);
            Assert.Equal(sentHash, receivedHash);
        }

        // ──────────────────────────────────────────────
        // Rapid complete after write doesn't lose data
        // ──────────────────────────────────────────────

        [Fact]
        public void Complete_Immediately_After_Write_Does_Not_Lose_Data()
        {
            using var buf = new CircularWriteBuffer(256);
            var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

            buf.Write(data);
            buf.Complete();

            Assert.True(buf.HasData);
            buf.GetReadableRegions(out var seg1, out _, out var total);
            Assert.Equal(4, total);
            Assert.Equal(data, seg1.ToArray());
        }

        // ──────────────────────────────────────────────
        // Simulate real H2 WriteLoop pattern
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Simulated_H2_WriteLoop_Pattern()
        {
            const int cap = 32768;
            const int frameCount = 500;
            using var tb = new TestableBuffer(cap);

            var allFrames = new List<byte[]>();

            // Simulate H2 WriteLoop consumer
            var consumerTask = Task.Run(async () =>
            {
                var collected = new List<byte>();

                while (await tb.WaitForDataAsync(CancellationToken.None)) {
                    tb.Buffer.GetReadableRegions(out var seg1, out var seg2, out var total);

                    // Simulate writing to stream (just collect bytes)
                    if (seg1.Length > 0)
                        collected.AddRange(seg1.ToArray());

                    if (!seg2.IsEmpty)
                        collected.AddRange(seg2.ToArray());

                    tb.Buffer.Advance(total);
                }

                return collected.ToArray();
            });

            // Simulate multiple stream workers writing frames concurrently
            var expectedTotal = 0;
            var producers = Enumerable.Range(0, 4).Select(streamId => Task.Run(() =>
            {
                for (var i = 0; i < frameCount; i++) {
                    // Simulate a DATA frame: 9-byte header + variable payload
                    var payloadSize = (streamId * 7 + i * 3) % 50 + 1;
                    var frame = new byte[9 + payloadSize];

                    // Tag the frame with stream ID and sequence for identification
                    frame[0] = 0xDA; // "DATA" marker
                    frame[1] = (byte)streamId;
                    frame[2] = (byte)(i & 0xFF);
                    frame[3] = (byte)((i >> 8) & 0xFF);

                    // Fill payload with deterministic pattern
                    for (var j = 9; j < frame.Length; j++)
                        frame[j] = (byte)((streamId + i + j) & 0xFF);

                    tb.Buffer.Write(frame);
                    Interlocked.Add(ref expectedTotal, frame.Length);
                }
            })).ToArray();

            await Task.WhenAll(producers);
            tb.Buffer.Complete();

            var result = await consumerTask.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(expectedTotal, result.Length);
        }
    }
}
