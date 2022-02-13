using System;
using Echoes.H2.Encoder.HPack;
using Echoes.H2.Encoder.Utils.Interfaces;

namespace Echoes.H2.Encoder
{
    public class DecodingContext
    {
        private readonly HPackDecodingDynamicTable _dynamicTable; 

        private readonly IMemoryProvider<char> _memoryProvider;

        public DecodingContext(
            IMemoryProvider<char> memoryProvider, 
            int maxDynamicTableSize = 1024 * 4)
        {
            _memoryProvider = memoryProvider;
            _dynamicTable = new HPackDecodingDynamicTable(maxDynamicTableSize);
        }

        internal HeaderField [] DynContent()
        {
            return _dynamicTable.GetContent(); 
        }
        
        internal HeaderField Register(ReadOnlySpan<char> headerName, ReadOnlySpan<char> headerValue)
        {
            var entry = new HeaderField(headerName, headerValue, _memoryProvider);
            _dynamicTable.Add(entry);
            return entry;
        }

        public void UpdateMaxSize(int newMaxSize)
        {
            _dynamicTable.UpdateMaxSize(newMaxSize);
        }

        public bool TryGetEntry(int externalIndex, out HeaderField tableEntry)
        {
            var indexStaticTable = externalIndex - 1; 

            if ((indexStaticTable) < HPackStaticTableEntry.StaticTable.Length)
            {
                tableEntry = HPackStaticTableEntry.StaticTable[indexStaticTable]; 
                return true;
            }

            return _dynamicTable.TryGet(externalIndex, out tableEntry);
        }
        
    }
}