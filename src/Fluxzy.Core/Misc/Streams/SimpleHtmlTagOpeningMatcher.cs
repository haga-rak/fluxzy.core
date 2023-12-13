// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    ///     A trivial state-machine to detect opening tag in html. Used essentially to detect the header tag
    /// </summary>
    public class SimpleHtmlTagOpeningMatcher : StringMatcher
    {
        private readonly bool _replace;
        private readonly StringComparison _stringComparison;

        public SimpleHtmlTagOpeningMatcher(Encoding encoding, StringComparison stringComparison, bool replace)
            : base(encoding, stringComparison)
        {
            _stringComparison = stringComparison;
            _replace = replace;
        }

        public override (int Index, int Count) FindIndex(ReadOnlySpan<char> buffer, ReadOnlySpan<char> searchText)
        {
            var state = DetectingState.None;
            var index = 0;

            var validatedSearchTextIndex = -1;

            Span<char> compareBuffer = stackalloc char[1];

            for (var i = 0; i < buffer.Length; i++) {
                var c = buffer[i];

                switch (state) {
                    case DetectingState.None:

                        validatedSearchTextIndex = -1;

                        if (c == '<') {
                            state = DetectingState.WaitingTagName;
                            index = i;
                        }

                        break;

                    case DetectingState.WaitingTagName:

                        if (validatedSearchTextIndex == -1 &&
                            (c == ' ' || c == '\t')) // add other character here if needed 
                        {
                            continue;
                        }

                        if (validatedSearchTextIndex == -1) {
                            validatedSearchTextIndex = 0;
                        }

                        if (validatedSearchTextIndex >= searchText.Length) {
                            if (c == ' ' || c == '\t') {
                                // Search end 
                                state = DetectingState.WaitingTagClose;

                                continue;
                            }

                            if (c == '>') {
                                return (index, i - index + 1);
                            }

                            state = DetectingState.None;

                            continue;
                        }

                        compareBuffer[0] = c;

                        if (((ReadOnlySpan<char>) compareBuffer).Equals(
                                searchText.Slice(validatedSearchTextIndex++, 1),
                                _stringComparison)) {
                            continue;
                        }

                        state = DetectingState.None;

                        break;

                    case DetectingState.WaitingTagClose:
                        if (c == '>') {
                            return (index, i - index + 1);
                        }

                        break;
                }
            }

            return (-1, 0);
        }

        protected override BinaryMatchResult GetMatchValue(int index, int blockLength, int shiftLength)
        {
            return _replace
                ? new BinaryMatchResult(index, blockLength, 0)
                : new BinaryMatchResult(index, blockLength, shiftLength);
        }

        internal enum DetectingState
        {
            None = 0,
            WaitingTagName,
            WaitingTagClose
        }
    }
}
