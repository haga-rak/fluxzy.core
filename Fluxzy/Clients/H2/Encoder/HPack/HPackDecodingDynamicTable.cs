// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Clients.H2.Encoder.HPack
{
    public class HPackDecodingDynamicTable
    {
        private readonly Dictionary<int, HeaderField> _entries = new();

        private int _currentMaxSize;
        private int _currentSize;

        private int _internalIndex = -1;
        private int _oldestElementInternalIndex;

        public HPackDecodingDynamicTable(int initialSize)
        {
            _currentMaxSize = initialSize;
        }

        internal H2Logger? Logger { get; set; }

        private int EvictUntil(int toBeRemovedSize)
        {
            var evictedSize = 0;
            int i;

            for (i = _oldestElementInternalIndex; evictedSize < toBeRemovedSize; i++) {
                if (!_entries.TryGetValue(i, out var tableEntry)) {
                    _oldestElementInternalIndex = _internalIndex; // There's no more element on the list 

                    return evictedSize;
                }

                _entries.Remove(i);

                _currentSize -= tableEntry.Size;
                evictedSize += tableEntry.Size;
            }

            _oldestElementInternalIndex = i;

            return evictedSize;
        }

        public HeaderField[] GetContent()
        {
            return _entries.Values.OrderBy(r => r.Name.ToString()).ToArray();
        }

        public void UpdateMaxSize(int newMaxSize)
        {
            var tobeRemovedSize = _currentSize - newMaxSize;

            if (tobeRemovedSize > 0)
                EvictUntil(tobeRemovedSize);

            _currentMaxSize = newMaxSize;
        }

        private int ConvertIndexToInternal(int externalIndex)
        {
            var temp = externalIndex - 61 - 1; // Extra -1 because externalIndex starts with 1

            return _entries.Count - 1 - temp + _oldestElementInternalIndex;
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

                var evictedSize = EvictUntil(spaceNeeded);

                // No decoding error.
                // Inserting element larger than Table MAX SIZE cause the table to be emptied 

                if (evictedSize < spaceNeeded) {
                    _currentSize = 0;
                    _entries.Clear();
                    _internalIndex = -1;
                    _oldestElementInternalIndex = 0;

                    return _internalIndex;
                }
            }

            _currentSize += entry.Size;

            _internalIndex += 1;

            _entries[_internalIndex] = entry;

            return _internalIndex;
        }

        public bool TryGet(int externalIndex, out HeaderField entry)
        {
            return _entries.TryGetValue(ConvertIndexToInternal(externalIndex), out entry);
        }
    }
}
