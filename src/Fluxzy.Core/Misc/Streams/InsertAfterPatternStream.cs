// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    ///   
    /// </summary>
    public class InsertAfterPatternStream : Stream
    {
        public InsertAfterPatternStream(Stream innerStream, string searchPattern, string replacement)
        {
            
        }
        
        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override bool CanRead { get; }

        public override bool CanSeek { get; }

        public override bool CanWrite { get; }

        public override long Length { get; }

        public override long Position { get; set; }
    }

    public interface IBinaryMatcher
    {
        int FindIndex(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> searchText); 
    }
    
    public class RawByteBinaryMatcher : IBinaryMatcher
    {
        public int FindIndex(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> searchText)
        {
            return buffer.IndexOf(searchText);
        }
    }
    
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
