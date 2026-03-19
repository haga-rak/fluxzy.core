// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.HPack;
using Xunit;

namespace Fluxzy.Tests.UnitTests.HPack
{
    public class HPackEncodingDynamicTableTests
    {
        private static HeaderField H(string name, string value) => new(name, value);

        [Fact]
        public void Add_FirstEntry_ReturnsIndex62()
        {
            var table = new HPackEncodingDynamicTable(4096);
            var idx = table.Add(H("content-type", "text/html"));
            Assert.Equal(62, idx);
        }

        [Fact]
        public void Add_MultipleEntries_IndicesShift()
        {
            var table = new HPackEncodingDynamicTable(4096);

            var idx1 = table.Add(H("content-type", "text/html"));
            Assert.Equal(62, idx1);

            var idx2 = table.Add(H("content-length", "100"));
            Assert.Equal(62, idx2); // newest is always 62

            // First entry should now be at 63 via TryGet
            Assert.True(table.TryGet(H("content-type", "text/html"), out var extIdx));
            Assert.Equal(63, extIdx);
        }

        [Fact]
        public void TryGet_ExistingEntry_ReturnsCorrectIndex()
        {
            var table = new HPackEncodingDynamicTable(4096);
            table.Add(H("x-custom", "value1"));
            table.Add(H("x-other", "value2"));
            table.Add(H("x-third", "value3"));

            Assert.True(table.TryGet(H("x-custom", "value1"), out var idx));
            Assert.Equal(64, idx); // oldest of 3 entries

            Assert.True(table.TryGet(H("x-other", "value2"), out idx));
            Assert.Equal(63, idx);

            Assert.True(table.TryGet(H("x-third", "value3"), out idx));
            Assert.Equal(62, idx); // newest
        }

        [Fact]
        public void TryGet_MissingEntry_ReturnsFalse()
        {
            var table = new HPackEncodingDynamicTable(4096);
            table.Add(H("x-custom", "value1"));

            Assert.False(table.TryGet(H("x-missing", "nope"), out _));
        }

        [Fact]
        public void Add_CausesEviction_RemovesOldest()
        {
            // Small table: 32 + name.Length + value.Length per entry
            // Entry "ab":"cd" = 32 + 2 + 2 = 36 bytes
            // Table size 80 fits 2 entries (72), third entry causes eviction of first
            var table = new HPackEncodingDynamicTable(80);

            table.Add(H("aa", "11"));
            table.Add(H("bb", "22"));

            // Both should exist
            Assert.True(table.TryGet(H("aa", "11"), out _));
            Assert.True(table.TryGet(H("bb", "22"), out _));

            // Adding third should evict "aa":"11"
            table.Add(H("cc", "33"));
            Assert.False(table.TryGet(H("aa", "11"), out _));
            Assert.True(table.TryGet(H("bb", "22"), out _));
            Assert.True(table.TryGet(H("cc", "33"), out _));
        }

        [Fact]
        public void Add_OversizedEntry_ClearsTable_ReturnsMinus1()
        {
            var table = new HPackEncodingDynamicTable(64);
            table.Add(H("a", "b")); // 32 + 1 + 1 = 34

            // Add entry larger than max size
            var bigValue = new string('x', 100); // 32 + 1 + 100 = 133 > 64
            var result = table.Add(H("z", bigValue));

            Assert.Equal(-1, result);
            Assert.False(table.TryGet(H("a", "b"), out _));
        }

        [Fact]
        public void UpdateMaxSize_Shrink_EvictsEntries()
        {
            var table = new HPackEncodingDynamicTable(4096);

            table.Add(H("aa", "11")); // 36
            table.Add(H("bb", "22")); // 36
            table.Add(H("cc", "33")); // 36 — total 108

            // Shrink to fit only 2 entries
            table.UpdateMaxSize(80);

            Assert.False(table.TryGet(H("aa", "11"), out _)); // evicted
            Assert.True(table.TryGet(H("bb", "22"), out _));
            Assert.True(table.TryGet(H("cc", "33"), out _));
        }

        [Fact]
        public void UpdateMaxSize_ToZero_ClearsTable()
        {
            var table = new HPackEncodingDynamicTable(4096);
            table.Add(H("aa", "11"));
            table.Add(H("bb", "22"));

            table.UpdateMaxSize(0);

            Assert.False(table.TryGet(H("aa", "11"), out _));
            Assert.False(table.TryGet(H("bb", "22"), out _));
        }

        [Fact]
        public void Add_DuplicateHeaders_BothExist()
        {
            var table = new HPackEncodingDynamicTable(4096);
            table.Add(H("x-dup", "val"));
            table.Add(H("x-dup", "val"));

            // TryGet returns the newest (last added) index
            Assert.True(table.TryGet(H("x-dup", "val"), out var idx));
            Assert.Equal(62, idx); // newest
        }

        [Fact]
        public void Add_AfterEviction_WorksCorrectly()
        {
            var table = new HPackEncodingDynamicTable(64);

            // Add oversized to clear
            var result = table.Add(H("z", new string('x', 100)));
            Assert.Equal(-1, result);

            // Add normal entry after clear
            var idx = table.Add(H("a", "b")); // 34 fits in 64
            Assert.Equal(62, idx);
            Assert.True(table.TryGet(H("a", "b"), out var extIdx));
            Assert.Equal(62, extIdx);
        }

        [Fact]
        public void Add_ManyEntries_RingWraps()
        {
            var table = new HPackEncodingDynamicTable(100000);

            for (var i = 0; i < 150; i++) {
                var idx = table.Add(H($"header-{i}", $"value-{i}"));
                Assert.Equal(62, idx); // newest always at 62
            }

            // Last added should be retrievable
            Assert.True(table.TryGet(H("header-149", "value-149"), out var extIdx));
            Assert.Equal(62, extIdx);

            // Several recent entries should exist
            Assert.True(table.TryGet(H("header-148", "value-148"), out extIdx));
            Assert.Equal(63, extIdx);

            var content = table.GetContent();
            Assert.True(content.Length > 0);
        }
    }
}
