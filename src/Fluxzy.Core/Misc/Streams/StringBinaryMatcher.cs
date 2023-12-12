// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Text;

namespace Fluxzy.Misc.Streams
{
    public class StringBinaryMatcher : IBinaryMatcher
    {
        private readonly Encoding _encoding;

        public StringBinaryMatcher(Encoding encoding)
        {
            _encoding = encoding;
        }
        
        public int FindIndex(ReadOnlySpan<byte> content, ReadOnlySpan<byte> searchText)
        {
            var searchTextStringCount = _encoding.GetCharCount(searchText);

            char[]? rawSearchTextBuffer = null;

            var searchTextArray = searchTextStringCount < FluxzySharedSetting.StackAllocThreshold
                ? stackalloc char[searchTextStringCount]
                : (rawSearchTextBuffer = ArrayPool<char>.Shared.Rent(searchTextStringCount));
            
            var contentTextStringCount = _encoding.GetCharCount(content);
            
            char[]? rawContentTextBuffer = null;
            
            var contentTextArray = contentTextStringCount < FluxzySharedSetting.StackAllocThreshold
                ? stackalloc char[contentTextStringCount]
                : (rawContentTextBuffer = ArrayPool<char>.Shared.Rent(contentTextStringCount));

            try {
                var searchTextCount = _encoding.GetChars(searchText, searchTextArray);
                var searchContentCount = _encoding.GetChars(content, contentTextArray);
                
                searchTextArray = searchTextArray.Slice(0, searchTextCount);
                contentTextArray = contentTextArray.Slice(0, searchContentCount);

                var contentTextSpan = contentTextArray.Slice(0, contentTextStringCount);
                var searchTextSpan = searchTextArray.Slice(0, searchTextStringCount);

                var charIndex = contentTextSpan.IndexOf(searchTextSpan);
                
                if (charIndex < 0)
                    return -1;
                
                var byteIndex = _encoding.GetByteCount(contentTextSpan.Slice(0, charIndex));
                
                return byteIndex;
            }
            finally {
                if (rawSearchTextBuffer != null)
                    ArrayPool<char>.Shared.Return(rawSearchTextBuffer);
                
                if (rawContentTextBuffer != null)
                    ArrayPool<char>.Shared.Return(rawContentTextBuffer);
            }
        }
    }
}
