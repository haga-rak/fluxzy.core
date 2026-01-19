// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;

namespace Fluxzy.Cli.Commands.PrettyOutput
{
    /// <summary>
    /// A thread-safe circular buffer for storing exchange display entries.
    /// Uses a fixed-size array with lock-free read operations and minimal locking for writes.
    /// </summary>
    public class CircularExchangeBuffer
    {
        private readonly ExchangeDisplayEntry[] _buffer;
        private readonly int _capacity;
        private readonly object _writeLock = new();

        private int _head;
        private int _count;

        public CircularExchangeBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");

            _capacity = capacity;
            _buffer = new ExchangeDisplayEntry[capacity];
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Gets the current number of entries in the buffer.
        /// </summary>
        public int Count => Volatile.Read(ref _count);

        /// <summary>
        /// Gets the maximum capacity of the buffer.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Adds an entry to the buffer. If the buffer is full, the oldest entry is overwritten.
        /// </summary>
        public void Add(ExchangeDisplayEntry entry)
        {
            lock (_writeLock)
            {
                _buffer[_head] = entry;
                _head = (_head + 1) % _capacity;

                if (_count < _capacity)
                {
                    _count++;
                }
            }
        }

        /// <summary>
        /// Returns a snapshot of all entries in chronological order (oldest first).
        /// The returned array is a copy, safe for iteration while new entries are added.
        /// </summary>
        public ExchangeDisplayEntry[] GetSnapshot()
        {
            lock (_writeLock)
            {
                var count = _count;
                if (count == 0)
                    return Array.Empty<ExchangeDisplayEntry>();

                var result = new ExchangeDisplayEntry[count];

                if (count < _capacity)
                {
                    // Buffer not full yet, entries start at index 0
                    Array.Copy(_buffer, 0, result, 0, count);
                }
                else
                {
                    // Buffer is full, oldest entry is at _head
                    var firstPartLength = _capacity - _head;
                    Array.Copy(_buffer, _head, result, 0, firstPartLength);
                    Array.Copy(_buffer, 0, result, firstPartLength, _head);
                }

                return result;
            }
        }

        /// <summary>
        /// Returns a snapshot of the most recent N entries in chronological order.
        /// </summary>
        public ExchangeDisplayEntry[] GetRecentSnapshot(int maxEntries)
        {
            lock (_writeLock)
            {
                var count = Math.Min(_count, maxEntries);
                if (count == 0)
                    return Array.Empty<ExchangeDisplayEntry>();

                var result = new ExchangeDisplayEntry[count];

                // Calculate the starting position for the most recent 'count' entries
                var startIndex = (_head - count + _capacity) % _capacity;

                if (startIndex + count <= _capacity)
                {
                    // Entries are contiguous
                    Array.Copy(_buffer, startIndex, result, 0, count);
                }
                else
                {
                    // Entries wrap around
                    var firstPartLength = _capacity - startIndex;
                    Array.Copy(_buffer, startIndex, result, 0, firstPartLength);
                    Array.Copy(_buffer, 0, result, firstPartLength, count - firstPartLength);
                }

                return result;
            }
        }

        /// <summary>
        /// Clears all entries from the buffer.
        /// </summary>
        public void Clear()
        {
            lock (_writeLock)
            {
                Array.Clear(_buffer, 0, _capacity);
                _head = 0;
                _count = 0;
            }
        }
    }
}
