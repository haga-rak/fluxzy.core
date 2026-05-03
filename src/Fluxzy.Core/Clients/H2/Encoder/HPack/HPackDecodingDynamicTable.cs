// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;

namespace Fluxzy.Clients.H2.Encoder.HPack
{
    public class HPackDecodingDynamicTable
    {
        // Ring buffer for entries (FIFO order: oldest at _head, newest at tail)
        private HeaderField[] _ring;
        private int _head;  // index of oldest entry
        private int _count; // number of live entries

        private int _currentMaxSize;
        private int _currentSize;

        public HPackDecodingDynamicTable(int initialSize)
        {
            _currentMaxSize = initialSize;
            _ring = new HeaderField[Math.Max(16, initialSize / 32)];
        }
        private void EvictUntil(int toBeRemovedSize)
        {
            var evictedSize = 0;

            while (_count > 0 && evictedSize < toBeRemovedSize) {
                var entry = _ring[_head];
                _ring[_head] = default; // release reference
                _currentSize -= entry.Size;
                evictedSize += entry.Size;
                _head = (_head + 1) % _ring.Length;
                _count--;
            }
        }

        public HeaderField[] GetContent()
        {
            var result = new HeaderField[_count];
            for (var i = 0; i < _count; i++) {
                result[i] = _ring[(_head + i) % _ring.Length];
            }
            return result.OrderBy(r => r.Name.ToString()).ToArray();
        }

        public void UpdateMaxSize(int newMaxSize)
        {
            var tobeRemovedSize = _currentSize - newMaxSize;

            if (tobeRemovedSize > 0)
                EvictUntil(tobeRemovedSize);

            _currentMaxSize = newMaxSize;
        }

        /// <summary>
        ///     Adding new entry.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public int Add(in HeaderField entry)
        {
            var provisionalSize = _currentSize + entry.Size;

            if (provisionalSize > _currentMaxSize) {
                var spaceNeeded = provisionalSize - _currentMaxSize;
                EvictUntil(spaceNeeded);

                // Entry larger than table max size: empty the table
                if (_currentSize + entry.Size > _currentMaxSize) {
                    _currentSize = 0;
                    _count = 0;
                    _head = 0;
                    return -1;
                }
            }

            // Grow ring if full
            if (_count == _ring.Length) {
                GrowRing();
            }

            var tail = (_head + _count) % _ring.Length;
            _ring[tail] = entry;
            _count++;
            _currentSize += entry.Size;

            return _count - 1; // return value not used by callers, preserve non-negative on success
        }

        private void GrowRing()
        {
            var newCapacity = _ring.Length * 2;
            var newRing = new HeaderField[newCapacity];

            for (var i = 0; i < _count; i++) {
                newRing[i] = _ring[(_head + i) % _ring.Length];
            }

            _ring = newRing;
            _head = 0;
        }

        public bool TryGet(int externalIndex, out HeaderField entry)
        {
            // externalIndex: 62 = newest, 63 = second newest, etc.
            var offset = externalIndex - 62;

            if (offset < 0 || offset >= _count) {
                entry = default;
                return false;
            }

            // offset 0 = newest = tail-1, offset 1 = second newest, ...
            var ringIndex = (_head + _count - 1 - offset) % _ring.Length;
            entry = _ring[ringIndex];
            return true;
        }
    }
}
