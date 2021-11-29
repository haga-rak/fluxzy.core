using System.Collections.Generic;
using System.Linq;
using Echoes.Encoding.Utils;

namespace Echoes.Encoding.HPack
{
    public class HPackEncodingDynamicTable
    {
        private readonly Dictionary<int, HeaderField> _entries = new Dictionary<int, HeaderField>();
        private readonly Dictionary<HeaderField, int> _reverseEntries = new Dictionary<HeaderField, int>(new TableEntryComparer()); // only used for evicting

        private int _currentMaxSize;
        private int _currentSize;

        private int _internalIndex = -1;
        private int _oldestElementInternalIndex = 0;

        public HPackEncodingDynamicTable(int initialSize)
        {
            _currentMaxSize = initialSize;
        }

        public HeaderField[] GetContent()
        {
            return _entries.Values.OrderBy(r => r.Name.ToString()).ToArray(); 
        }

        private int EvictUntil(int toBeRemovedSize)
        {
            var evictedSize = 0;
            var i = 0;

            for (i = _oldestElementInternalIndex; evictedSize < toBeRemovedSize; i++)
            {
                if (!_entries.TryGetValue(i, out var tableEntry))
                {
                    _oldestElementInternalIndex = _internalIndex; // There's no more element on the list 
                    return evictedSize;
                }

                _entries.Remove(i);
                _reverseEntries.Remove(tableEntry);

                _currentSize -= tableEntry.Size;
                evictedSize += tableEntry.Size;
            }

            _oldestElementInternalIndex = i;

            return evictedSize;
        }

        private int ConvertIndexFromInternal(int internalIndex)
        {
            return ((_entries.Count) - (internalIndex - _oldestElementInternalIndex)) + 61;
        }

        public void UpdateMaxSize(int newMaxSize)
        {
            var tobeRemovedSize = _currentSize - newMaxSize;

            if (tobeRemovedSize > 0)
            {
                EvictUntil(tobeRemovedSize); 
            }

            _currentMaxSize = newMaxSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>Return new entry index</returns>
        public int Add(in HeaderField entry)
        {
            var provisionalSize = _currentSize + entry.Size;

            if (provisionalSize > _currentMaxSize)
            {
                var evictedSize = EvictUntil(provisionalSize - _currentMaxSize);

                // No decoding error.
                // Inserting element larger than Table MAX SIZE cause the table to be emptied 
                if (evictedSize < entry.Size)
                    return -1;
            }

            _currentSize += entry.Size;

            _internalIndex += 1;

            _entries[_internalIndex] = entry;
            _reverseEntries[entry] = _internalIndex;

            return ConvertIndexFromInternal(_internalIndex);
        }

        public bool TryGet(in HeaderField entry, out int indexExternal)
        {
            int indexInternal;

            if (_reverseEntries.TryGetValue(entry, out indexInternal))
            {
                indexExternal = ConvertIndexFromInternal(indexInternal);
                return true; 
            }

            indexExternal = -1; 
            return false; 
        }
    }
}