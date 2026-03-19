// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Clients.H2.Encoder.HPack
{
    public class HPackEncodingDynamicTable
    {
        // Ring buffer for entries (FIFO order: oldest at _head, newest at _tail-1)
        private HeaderField[] _ring;
        private int _head;  // index of oldest entry
        private int _count; // number of live entries

        private readonly Dictionary<HeaderField, int>
            _reverseEntries = new(new TableEntryComparer());

        private int _currentMaxSize;
        private int _currentSize;

        // Monotonically increasing insertion index (used as value in _reverseEntries)
        private int _insertionIndex;

        public HPackEncodingDynamicTable(int initialSize)
        {
            _currentMaxSize = initialSize;
            _ring = new HeaderField[Math.Max(16, initialSize / 32)];
        }

        public HeaderField[] GetContent()
        {
            var result = new HeaderField[_count];
            for (var i = 0; i < _count; i++) {
                result[i] = _ring[(_head + i) % _ring.Length];
            }
            return result.OrderBy(r => r.Name.ToString()).ToArray();
        }

        private void EvictUntil(int toBeRemovedSize)
        {
            var evictedSize = 0;

            while (_count > 0 && evictedSize < toBeRemovedSize) {
                var entry = _ring[_head];
                _reverseEntries.Remove(entry);
                _ring[_head] = default; // release reference
                _currentSize -= entry.Size;
                evictedSize += entry.Size;
                _head = (_head + 1) % _ring.Length;
                _count--;
            }
        }

        private int ConvertInsertionIndexToExternal(int insertionIdx)
        {
            // Newest entry = external index 62 (first dynamic table entry)
            // The offset from newest: _insertionIndex - insertionIdx
            // External index: 62 + offset = 62 + (_insertionIndex - insertionIdx)
            // But _insertionIndex is already incremented after Add, so:
            // the last inserted entry has insertionIdx = _insertionIndex - 1
            // and should get external index 62.
            return 62 + (_insertionIndex - 1 - insertionIdx);
        }

        public void UpdateMaxSize(int newMaxSize)
        {
            var tobeRemovedSize = _currentSize - newMaxSize;

            if (tobeRemovedSize > 0)
                EvictUntil(tobeRemovedSize);

            _currentMaxSize = newMaxSize;
        }

        /// <summary>
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>Return new entry index</returns>
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
                    _reverseEntries.Clear();
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

            var idx = _insertionIndex;
            _insertionIndex++;

            _reverseEntries[entry] = idx;

            return ConvertInsertionIndexToExternal(idx);
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

        public bool TryGet(in HeaderField entry, out int indexExternal)
        {
            if (_reverseEntries.TryGetValue(entry, out var insertionIdx)) {
                indexExternal = ConvertInsertionIndexToExternal(insertionIdx);
                return true;
            }

            indexExternal = -1;
            return false;
        }
    }
}
