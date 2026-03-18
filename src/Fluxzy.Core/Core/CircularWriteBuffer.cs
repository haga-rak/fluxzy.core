// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    /// <summary>
    ///     A single-allocation, multi-producer / single-consumer circular byte buffer.
    ///     Replaces Channel&lt;PooledFrame&gt; + coalesce buffer in H2DownStreamPipe.
    ///     Producers write frame bytes directly into the ring; the consumer reads
    ///     contiguous regions and writes them to the downstream stream.
    /// </summary>
    internal sealed class CircularWriteBuffer : IDisposable
    {
        private readonly byte[] _buffer;
        private readonly int _capacity;
        private readonly object _lock = new();
        private readonly SemaphoreSlim _dataReady = new(0, 1);

        private int _head;          // Next write position (producer-side)
        private int _tail;          // Next read position (consumer-side)
        private int _count;         // Bytes currently in buffer
        private int _signalState;   // 0 = consumer may be waiting, 1 = signaled
        private bool _completed;

        public CircularWriteBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity;
            _buffer = GC.AllocateUninitializedArray<byte>(capacity);
        }

        /// <summary>
        ///     Write frame bytes into the ring buffer.
        ///     Thread-safe for multiple concurrent producers.
        ///     Blocks if the buffer is full (back-pressure from slow consumer).
        /// </summary>
        public void Write(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
                return;

            if (data.Length > _capacity)
                throw new InvalidOperationException(
                    $"Frame size {data.Length} exceeds ring buffer capacity {_capacity}");

            lock (_lock) {
                // Wait for sufficient free space
                while (_capacity - _count < data.Length) {
                    if (_completed)
                        return;

                    Monitor.Wait(_lock);
                }

                if (_completed)
                    return;

                var spaceAtEnd = _capacity - _head;

                if (spaceAtEnd >= data.Length) {
                    data.CopyTo(_buffer.AsSpan(_head));
                    _head += data.Length;

                    if (_head == _capacity)
                        _head = 0;
                }
                else {
                    // Split write across the wrap boundary
                    data.Slice(0, spaceAtEnd).CopyTo(_buffer.AsSpan(_head));
                    data.Slice(spaceAtEnd).CopyTo(_buffer);
                    _head = data.Length - spaceAtEnd;
                }

                _count += data.Length;
            }

            // Signal consumer outside the lock
            SignalConsumer();
        }

        /// <summary>
        ///     Wait asynchronously for data to become available.
        ///     Returns true if data is available, false if the buffer is completed and empty.
        ///     Single-consumer only.
        /// </summary>
        public async ValueTask<bool> WaitForDataAsync(CancellationToken ct)
        {
            while (true) {
                lock (_lock) {
                    if (_count > 0)
                        return true;

                    if (_completed)
                        return false;

                    // Reset signal state under lock so any subsequent Write will re-signal
                    Interlocked.Exchange(ref _signalState, 0);
                }

                await _dataReady.WaitAsync(ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Get the current readable regions. May return two segments if data wraps.
        ///     Single-consumer only. Does not advance the tail; call <see cref="Advance"/> after writing.
        /// </summary>
        public void GetReadableRegions(
            out ReadOnlyMemory<byte> first,
            out ReadOnlyMemory<byte> second,
            out int totalBytes)
        {
            lock (_lock) {
                totalBytes = _count;

                if (_count == 0) {
                    first = default;
                    second = default;
                    return;
                }

                var contiguous = _capacity - _tail;

                if (contiguous >= _count) {
                    first = new ReadOnlyMemory<byte>(_buffer, _tail, _count);
                    second = default;
                }
                else {
                    first = new ReadOnlyMemory<byte>(_buffer, _tail, contiguous);
                    second = new ReadOnlyMemory<byte>(_buffer, 0, _count - contiguous);
                }
            }
        }

        /// <summary>
        ///     Mark bytes as consumed, freeing space for producers.
        ///     Single-consumer only.
        /// </summary>
        public void Advance(int count)
        {
            lock (_lock) {
                _tail = (_tail + count) % _capacity;
                _count -= count;

                // Wake any producers blocked waiting for space
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        ///     Signal that no more data will be written.
        /// </summary>
        public void Complete()
        {
            lock (_lock) {
                _completed = true;
                Monitor.PulseAll(_lock);
            }

            SignalConsumer();
        }

        private void SignalConsumer()
        {
            if (Interlocked.CompareExchange(ref _signalState, 1, 0) == 0) {
                _dataReady.Release();
            }
        }

        public void Dispose()
        {
            Complete();
            _dataReady.Dispose();
        }
    }
}
