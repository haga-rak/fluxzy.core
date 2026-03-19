// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.HPack;
using Fluxzy.Clients.H2.Encoder.Utils;
using Xunit;

namespace Fluxzy.Tests.UnitTests.HPack
{
    public class HPackDynamicTableEquivalenceTests
    {
        private static readonly string[] Names =
        {
            ":method", ":path", ":scheme", ":status", ":authority",
            "content-type", "content-length", "accept", "accept-encoding", "accept-language",
            "cache-control", "cookie", "host", "user-agent", "x-custom-header",
            "x-request-id", "authorization", "date", "server", "vary"
        };

        private static readonly string[] Values =
        {
            "GET", "POST", "/", "/index.html", "https",
            "text/html", "application/json", "gzip", "en-US", "no-cache"
        };

        private static HeaderField RandomHeader(Random rng)
        {
            var name = Names[rng.Next(Names.Length)];
            var value = Values[rng.Next(Values.Length)];
            return new HeaderField(name, value);
        }

        #region Encoding Equivalence

        [Theory]
        [InlineData(42)]
        [InlineData(123)]
        [InlineData(999)]
        [InlineData(7777)]
        [InlineData(31415)]
        public void EncodingTable_Equivalence(int seed)
        {
            const int tableSize = 4096;
            const int opCount = 200;

            var rng = new Random(seed);
            var reference = new ReferenceEncodingDynamicTable(tableSize);
            var actual = new HPackEncodingDynamicTable(tableSize);

            // Pre-generate all operations
            var ops = new List<(int type, HeaderField header, int maxSize)>();
            for (var i = 0; i < opCount; i++) {
                var roll = rng.Next(100);
                if (roll < 70) {
                    ops.Add((0, RandomHeader(rng), 0)); // Add
                }
                else if (roll < 90) {
                    ops.Add((1, RandomHeader(rng), 0)); // TryGet
                }
                else {
                    ops.Add((2, default, rng.Next(8193))); // UpdateMaxSize
                }
            }

            for (var i = 0; i < ops.Count; i++) {
                var (type, header, maxSize) = ops[i];

                switch (type) {
                    case 0: { // Add
                        var refResult = reference.Add(header);
                        var actResult = actual.Add(header);
                        Assert.Equal(refResult, actResult);
                        break;
                    }
                    case 1: { // TryGet
                        var refFound = reference.TryGet(header, out var refIdx);
                        var actFound = actual.TryGet(header, out var actIdx);
                        Assert.Equal(refFound, actFound);
                        if (refFound)
                            Assert.Equal(refIdx, actIdx);
                        break;
                    }
                    case 2: { // UpdateMaxSize
                        reference.UpdateMaxSize(maxSize);
                        actual.UpdateMaxSize(maxSize);
                        break;
                    }
                }
            }

            // Final content comparison — sort by name+value for deterministic order
            var refContent = reference.GetContent()
                .OrderBy(r => r.Name.ToString()).ThenBy(r => r.Value.ToString()).ToArray();
            var actContent = actual.GetContent()
                .OrderBy(r => r.Name.ToString()).ThenBy(r => r.Value.ToString()).ToArray();
            Assert.Equal(refContent.Length, actContent.Length);
            for (var i = 0; i < refContent.Length; i++) {
                Assert.Equal(refContent[i].Name.ToString(), actContent[i].Name.ToString());
                Assert.Equal(refContent[i].Value.ToString(), actContent[i].Value.ToString());
            }
        }

        #endregion

        #region Decoding Equivalence

        [Theory]
        [InlineData(42)]
        [InlineData(123)]
        [InlineData(999)]
        [InlineData(7777)]
        [InlineData(31415)]
        public void DecodingTable_Equivalence(int seed)
        {
            const int tableSize = 4096;
            const int opCount = 200;

            var rng = new Random(seed);
            var reference = new ReferenceDecodingDynamicTable(tableSize);
            var actual = new HPackDecodingDynamicTable(tableSize);

            var ops = new List<(int type, HeaderField header, int externalIndex, int maxSize)>();
            for (var i = 0; i < opCount; i++) {
                var roll = rng.Next(100);
                if (roll < 70) {
                    ops.Add((0, RandomHeader(rng), 0, 0)); // Add
                }
                else if (roll < 90) {
                    ops.Add((1, default, rng.Next(60, 80), 0)); // TryGet with index around dynamic table range
                }
                else {
                    ops.Add((2, default, 0, rng.Next(8193))); // UpdateMaxSize
                }
            }

            for (var i = 0; i < ops.Count; i++) {
                var (type, header, externalIndex, maxSize) = ops[i];

                switch (type) {
                    case 0: { // Add
                        var refResult = reference.Add(header);
                        var actResult = actual.Add(header);
                        // Both return -1 on oversized
                        if (refResult == -1)
                            Assert.Equal(-1, actResult);
                        else
                            Assert.True(actResult >= 0, $"Op {i}: ref returned {refResult}, actual returned {actResult}");
                        break;
                    }
                    case 1: { // TryGet
                        var refFound = reference.TryGet(externalIndex, out var refEntry);
                        var actFound = actual.TryGet(externalIndex, out var actEntry);
                        Assert.Equal(refFound, actFound);
                        if (refFound) {
                            Assert.Equal(refEntry.Name.ToString(), actEntry.Name.ToString());
                            Assert.Equal(refEntry.Value.ToString(), actEntry.Value.ToString());
                        }
                        break;
                    }
                    case 2: { // UpdateMaxSize
                        reference.UpdateMaxSize(maxSize);
                        actual.UpdateMaxSize(maxSize);
                        break;
                    }
                }
            }

            // Final content comparison — sort by name+value for deterministic order
            var refContent = reference.GetContent()
                .OrderBy(r => r.Name.ToString()).ThenBy(r => r.Value.ToString()).ToArray();
            var actContent = actual.GetContent()
                .OrderBy(r => r.Name.ToString()).ThenBy(r => r.Value.ToString()).ToArray();
            Assert.Equal(refContent.Length, actContent.Length);
            for (var i = 0; i < refContent.Length; i++) {
                Assert.Equal(refContent[i].Name.ToString(), actContent[i].Name.ToString());
                Assert.Equal(refContent[i].Value.ToString(), actContent[i].Value.ToString());
            }
        }

        #endregion

        #region Reference Implementations (Dictionary-based originals)

        /// <summary>
        /// Original Dictionary-based encoding dynamic table for equivalence testing.
        /// </summary>
        private class ReferenceEncodingDynamicTable
        {
            private readonly Dictionary<int, HeaderField> _entries = new();

            private readonly Dictionary<HeaderField, int>
                _reverseEntries = new(new TableEntryComparer());

            private int _currentMaxSize;
            private int _currentSize;

            private int _internalIndex = -1;
            private int _oldestElementInternalIndex;

            public ReferenceEncodingDynamicTable(int initialSize)
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
                int i;

                for (i = _oldestElementInternalIndex; evictedSize < toBeRemovedSize; i++) {
                    if (!_entries.TryGetValue(i, out var tableEntry)) {
                        _oldestElementInternalIndex = _internalIndex;
                        return evictedSize;
                    }

                    _reverseEntries.Remove(tableEntry);
                    _entries.Remove(i);

                    _currentSize -= tableEntry.Size;
                    evictedSize += tableEntry.Size;
                }

                _oldestElementInternalIndex = i;
                return evictedSize;
            }

            private int ConvertIndexToExternal(int internalIndex)
            {
                var temp = internalIndex - _oldestElementInternalIndex;
                return _entries.Count - 1 - temp + 62;
            }

            public void UpdateMaxSize(int newMaxSize)
            {
                var tobeRemovedSize = _currentSize - newMaxSize;

                if (tobeRemovedSize > 0)
                    EvictUntil(tobeRemovedSize);

                _currentMaxSize = newMaxSize;
            }

            public int Add(in HeaderField entry)
            {
                var provisionalSize = _currentSize + entry.Size;

                if (provisionalSize > _currentMaxSize) {
                    var spaceNeeded = provisionalSize - _currentMaxSize;

                    var evictedSize = EvictUntil(spaceNeeded);

                    if (evictedSize < spaceNeeded) {
                        _currentSize = 0;
                        _entries.Clear();
                        _reverseEntries.Clear();
                        _internalIndex = -1;
                        _oldestElementInternalIndex = 0;

                        return -1;
                    }
                }

                _currentSize += entry.Size;

                _internalIndex += 1;

                _entries[_internalIndex] = entry;
                _reverseEntries[entry] = _internalIndex;

                return ConvertIndexToExternal(_internalIndex);
            }

            public bool TryGet(in HeaderField entry, out int indexExternal)
            {
                if (_reverseEntries.TryGetValue(entry, out var internalIndex)) {
                    indexExternal = ConvertIndexToExternal(internalIndex);
                    return true;
                }

                indexExternal = -1;
                return false;
            }
        }

        /// <summary>
        /// Original Dictionary-based decoding dynamic table for equivalence testing.
        /// </summary>
        private class ReferenceDecodingDynamicTable
        {
            private readonly Dictionary<int, HeaderField> _entries = new();

            private int _currentMaxSize;
            private int _currentSize;

            private int _internalIndex = -1;
            private int _oldestElementInternalIndex;

            public ReferenceDecodingDynamicTable(int initialSize)
            {
                _currentMaxSize = initialSize;
            }

            private int EvictUntil(int toBeRemovedSize)
            {
                var evictedSize = 0;
                int i;

                for (i = _oldestElementInternalIndex; evictedSize < toBeRemovedSize; i++) {
                    if (!_entries.TryGetValue(i, out var tableEntry)) {
                        _oldestElementInternalIndex = _internalIndex;
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
                var temp = externalIndex - 61 - 1;
                return _entries.Count - 1 - temp + _oldestElementInternalIndex;
            }

            public int Add(in HeaderField entry)
            {
                var provisionalSize = _currentSize + entry.Size;

                if (provisionalSize > _currentMaxSize) {
                    var spaceNeeded = provisionalSize - _currentMaxSize;

                    var evictedSize = EvictUntil(spaceNeeded);

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

        #endregion
    }
}
