// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Text;

namespace Fluxzy.Misc.Streams
{
    public abstract class StringMatcher : IBinaryMatcher
    {
        private readonly Encoding _encoding;
        private readonly StringComparison _stringComparison;

        protected StringMatcher(Encoding encoding, StringComparison stringComparison)
        {
            _encoding = encoding;
            _stringComparison = stringComparison;
        }

        public BinaryMatchResult FindIndex(ReadOnlySpan<byte> content, ReadOnlySpan<byte> searchText)
        {
            var searchTextStringCount = _encoding.GetCharCount(searchText);

            char[]? rawSearchTextBuffer = null;

            var searchTextArray = searchTextStringCount < FluxzySharedSetting.StackAllocThreshold
                ? stackalloc char[searchTextStringCount]
                : rawSearchTextBuffer = ArrayPool<char>.Shared.Rent(searchTextStringCount);

            var contentTextStringCount = _encoding.GetCharCount(content);

            char[]? rawContentTextBuffer = null;

            var contentTextArray = contentTextStringCount < FluxzySharedSetting.StackAllocThreshold
                ? stackalloc char[contentTextStringCount]
                : rawContentTextBuffer = ArrayPool<char>.Shared.Rent(contentTextStringCount);

            try {
                var searchTextCount = _encoding.GetChars(searchText, searchTextArray);
                var searchContentCount = _encoding.GetChars(content, contentTextArray);

                searchTextArray = searchTextArray.Slice(0, searchTextCount);
                contentTextArray = contentTextArray.Slice(0, searchContentCount);

                var contentTextSpan = contentTextArray.Slice(0, contentTextStringCount);
                var searchTextSpan = searchTextArray.Slice(0, searchTextStringCount);

                var (charIndex, count) = FindIndex(contentTextSpan, searchTextSpan);

                if (charIndex < 0) {
                    return new BinaryMatchResult(-1, 0, 0);
                }

                var byteIndex = _encoding.GetByteCount(contentTextSpan.Slice(0, charIndex));

                return GetMatchValue(byteIndex, count, count);
            }
            finally {
                if (rawSearchTextBuffer != null) {
                    ArrayPool<char>.Shared.Return(rawSearchTextBuffer);
                }

                if (rawContentTextBuffer != null) {
                    ArrayPool<char>.Shared.Return(rawContentTextBuffer);
                }
            }
        }

        protected abstract BinaryMatchResult GetMatchValue(int index, int blockLength, int shiftLength);

        public virtual (int Index, int Count) FindIndex(ReadOnlySpan<char> buffer, ReadOnlySpan<char> searchText)
        {
            return (buffer.IndexOf(searchText, _stringComparison), searchText.Length);
        }
    }

    public class InsertAfterBinaryMatcher : StringMatcher
    {
        public InsertAfterBinaryMatcher(
            Encoding encoding, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
            : base(encoding, stringComparison)
        {
        }

        protected override BinaryMatchResult GetMatchValue(int index, int searchTextLength, int shiftLength)
        {
            return new BinaryMatchResult(index, searchTextLength, shiftLength);
        }
    }

    public class ReplaceBinaryMatcher : StringMatcher
    {
        public ReplaceBinaryMatcher(
            Encoding encoding, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
            : base(encoding, stringComparison)
        {
        }

        protected override BinaryMatchResult GetMatchValue(int index, int searchTextLength, int shiftLength)
        {
            return new BinaryMatchResult(index, searchTextLength, 0);
        }
    }
}
