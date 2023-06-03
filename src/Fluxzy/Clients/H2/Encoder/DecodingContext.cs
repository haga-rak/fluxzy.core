// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients.H2.Encoder.HPack;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;

namespace Fluxzy.Clients.H2.Encoder
{
    public class DecodingContext
    {
        private readonly HPackDecodingDynamicTable _dynamicTable;
        private readonly ArrayPoolMemoryProvider<char> _memoryProvider;

        public DecodingContext(
            Authority authority,
            ArrayPoolMemoryProvider<char> memoryProvider,
            int maxDynamicTableSize = 1024 * 4)
        {
            Authority = authority;
            _memoryProvider = memoryProvider;
            _dynamicTable = new HPackDecodingDynamicTable(maxDynamicTableSize);
        }

        public Authority Authority { get; }

        internal HeaderField[] DynContent()
        {
            return _dynamicTable.GetContent();
        }

        internal HeaderField Register(ReadOnlySpan<char> headerName, ReadOnlySpan<char> headerValue)
        {
            var entry = new HeaderField(headerName, headerValue, _memoryProvider);
            var value = _dynamicTable.Add(entry);

            return entry;
        }

        public void UpdateMaxSize(int newMaxSize)
        {
            _dynamicTable.UpdateMaxSize(newMaxSize);
        }

        public bool TryGetEntry(int externalIndex, out HeaderField tableEntry)
        {
            var indexStaticTable = externalIndex - 1;

            if (indexStaticTable < HPackStaticTableEntry.StaticTable.Length) {
                tableEntry = HPackStaticTableEntry.StaticTable[indexStaticTable];

                return true;
            }

            return _dynamicTable.TryGet(externalIndex, out tableEntry);
        }
    }
}
