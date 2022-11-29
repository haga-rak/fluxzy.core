// Copyright © 2022 Haga RAKOTOHARIVELO

using System.IO;
using System.Text;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.Misc
{
    public class SearchStreamTests
    {
        [Theory]
        [InlineData("abcdef","d", 3)]
        [InlineData("abcdef","a", 0)]
        [InlineData("abcdef","f", 5)]
        [InlineData("abcdef","z", -1)]
        public void Check_For_Valid_Index(string input, string searchString, long expectedIndex)
        {
            var inputStream = input.ToMemoryStream();
            var searchSequence = searchString.ToArray();

            var searchStream = new SearchStream(inputStream, searchSequence);

            searchStream.Drain();

            Assert.NotNull(searchStream.Result);
            Assert.Equal(expectedIndex, searchStream.Result!.OffsetFound);
        }
    }

    static class SearchStreamTestHelper
    {
        public static Stream ToMemoryStream(this string input)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(input));
        }

        public static byte[] ToArray(this string input)
        {
            return Encoding.UTF8.GetBytes(input);
        }
    }
}
