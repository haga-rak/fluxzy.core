// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.HPack;
using Xunit;

namespace Fluxzy.Tests.UnitTests.HPack
{
    public class HPackDecodingDynamicTableTests
    {
        private static HeaderField H(string name, string value) => new(name, value);

        [Fact]
        public void Add_ThenTryGet_NewestAtIndex62()
        {
            var table = new HPackDecodingDynamicTable(4096);
            table.Add(H("content-type", "text/html"));

            Assert.True(table.TryGet(62, out var entry));
            Assert.Equal("content-type", entry.Name.ToString());
            Assert.Equal("text/html", entry.Value.ToString());
        }

        [Fact]
        public void Add_Multiple_OlderEntriesAtHigherIndices()
        {
            var table = new HPackDecodingDynamicTable(4096);

            table.Add(H("first", "1"));
            table.Add(H("second", "2"));
            table.Add(H("third", "3"));

            // Newest at 62, oldest at 64
            Assert.True(table.TryGet(62, out var entry));
            Assert.Equal("third", entry.Name.ToString());

            Assert.True(table.TryGet(63, out entry));
            Assert.Equal("second", entry.Name.ToString());

            Assert.True(table.TryGet(64, out entry));
            Assert.Equal("first", entry.Name.ToString());
        }

        [Fact]
        public void TryGet_OutOfRange_ReturnsFalse()
        {
            var table = new HPackDecodingDynamicTable(4096);
            table.Add(H("only", "one"));

            // Only index 62 should work
            Assert.False(table.TryGet(61, out _)); // below range
            Assert.False(table.TryGet(63, out _)); // above count
            Assert.False(table.TryGet(100, out _));
        }

        [Fact]
        public void Add_CausesEviction_OldestRemoved()
        {
            // Entry "ab":"cd" = 32 + 2 + 2 = 36 bytes
            var table = new HPackDecodingDynamicTable(80);

            table.Add(H("aa", "11")); // 36
            table.Add(H("bb", "22")); // 36 — total 72

            Assert.True(table.TryGet(62, out _)); // bb
            Assert.True(table.TryGet(63, out _)); // aa

            // Third entry evicts "aa":"11"
            table.Add(H("cc", "33")); // needs 36, total would be 108 > 80

            Assert.True(table.TryGet(62, out var entry));
            Assert.Equal("cc", entry.Name.ToString());

            Assert.True(table.TryGet(63, out entry));
            Assert.Equal("bb", entry.Name.ToString());

            Assert.False(table.TryGet(64, out _)); // aa was evicted
        }

        [Fact]
        public void Add_OversizedEntry_ClearsTable()
        {
            var table = new HPackDecodingDynamicTable(64);
            table.Add(H("a", "b")); // 34

            var bigValue = new string('x', 100); // 133 > 64
            var result = table.Add(H("z", bigValue));

            Assert.Equal(-1, result);
            Assert.False(table.TryGet(62, out _));
        }

        [Fact]
        public void UpdateMaxSize_Shrink_EvictsOldest()
        {
            var table = new HPackDecodingDynamicTable(4096);

            table.Add(H("aa", "11")); // 36
            table.Add(H("bb", "22")); // 36
            table.Add(H("cc", "33")); // 36 — total 108

            table.UpdateMaxSize(80); // evicts "aa":"11"

            Assert.True(table.TryGet(62, out var entry));
            Assert.Equal("cc", entry.Name.ToString());

            Assert.True(table.TryGet(63, out entry));
            Assert.Equal("bb", entry.Name.ToString());

            Assert.False(table.TryGet(64, out _));
        }

        [Fact]
        public void UpdateMaxSize_ToZero_ClearsAll()
        {
            var table = new HPackDecodingDynamicTable(4096);
            table.Add(H("aa", "11"));
            table.Add(H("bb", "22"));

            table.UpdateMaxSize(0);

            Assert.False(table.TryGet(62, out _));
            Assert.False(table.TryGet(63, out _));
        }

        [Fact]
        public void Add_ManyEntries_RingWraps()
        {
            var table = new HPackDecodingDynamicTable(100000);

            for (var i = 0; i < 150; i++) {
                table.Add(H($"header-{i}", $"value-{i}"));
            }

            // Newest at 62
            Assert.True(table.TryGet(62, out var entry));
            Assert.Equal("header-149", entry.Name.ToString());

            // Second newest at 63
            Assert.True(table.TryGet(63, out entry));
            Assert.Equal("header-148", entry.Name.ToString());

            var content = table.GetContent();
            Assert.True(content.Length > 0);
        }

        [Fact]
        public void GetContent_ReturnsAllLiveEntries()
        {
            var table = new HPackDecodingDynamicTable(4096);
            table.Add(H("b-header", "2"));
            table.Add(H("a-header", "1"));
            table.Add(H("c-header", "3"));

            var content = table.GetContent();
            Assert.Equal(3, content.Length);
            // Sorted by name
            Assert.Equal("a-header", content[0].Name.ToString());
            Assert.Equal("b-header", content[1].Name.ToString());
            Assert.Equal("c-header", content[2].Name.ToString());
        }

        [Fact]
        public void Add_AfterClear_WorksCorrectly()
        {
            var table = new HPackDecodingDynamicTable(64);

            var result = table.Add(H("z", new string('x', 100)));
            Assert.Equal(-1, result);

            // Should work normally after clear
            table.Add(H("a", "b")); // 34 fits in 64
            Assert.True(table.TryGet(62, out var entry));
            Assert.Equal("a", entry.Name.ToString());
        }

        [Fact]
        public void TryGet_AfterEviction_OldEntryGone()
        {
            var table = new HPackDecodingDynamicTable(80);

            table.Add(H("aa", "11")); // 36
            table.Add(H("bb", "22")); // 36

            // Verify both exist
            Assert.True(table.TryGet(63, out var entry));
            Assert.Equal("aa", entry.Name.ToString());

            // Evict via add
            table.Add(H("cc", "33"));

            // Old entry gone, indices shifted
            Assert.False(table.TryGet(64, out _));
        }
    }
}
