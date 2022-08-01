using System;
using Fluxzy.Clients.H2.Encoder.HPack;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Clients.H2.Encoder
{
    public class EncodingContext
    {
        private readonly HPackEncodingDynamicTable _dynamicTable; 

        private readonly ArrayPoolMemoryProvider<char> _memoryProvider;

        internal EncodingContext(
            ArrayPoolMemoryProvider<char> memoryProvider, 
            int maxDynamicTableSize = 1024 * 4)
        {
            _memoryProvider = memoryProvider;
            _dynamicTable = new HPackEncodingDynamicTable(maxDynamicTableSize);
        }

        public void UpdateMaxSize(int newMaxSize)
        {
            _dynamicTable.UpdateMaxSize(newMaxSize);
        }


        internal HeaderField[] DynContent()
        {
            return _dynamicTable.GetContent();
        }

        public bool TryGetEntry(in ReadOnlyMemory<char> headerName, out int externalIndex)
        {
            if (HPackStaticTableEntry.ReverseStaticTable.TryGetValue(new HeaderField(headerName), out var indexInternal))
            {
                externalIndex = indexInternal  + 1;
                return true; 
            }

            if (_dynamicTable.TryGet(new HeaderField(headerName), out externalIndex))
            {
                return true; 
            }

            externalIndex = -1;
            return false; 
        }

        public bool TryGetEntry(in ReadOnlyMemory<char> headerName, in  ReadOnlyMemory<char> headerValue, out int externalIndex)
        {
            if (HPackStaticTableEntry.ReverseStaticTable.TryGetValue(new HeaderField(headerName, headerValue), out var indexInternal))
            {
                externalIndex = indexInternal + 1;
                return true;
            }

            if (_dynamicTable.TryGet(new HeaderField(headerName, headerValue), out externalIndex))
            {
                return true;
            }

            externalIndex = -1;
            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="headerName"></param>
        /// <param name="headerValue"></param>
        /// <returns>Returns external index</returns>
        internal int Register(ReadOnlySpan<char> headerName, ReadOnlySpan<char> headerValue)
        {
            var s =  _dynamicTable.Add(new HeaderField(headerName, headerValue, _memoryProvider));

            return s; 
        }
    }
}