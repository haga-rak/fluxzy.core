// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.Misc
{
    public class SearchStreamTests
    {
        [Theory]
        [MemberData(nameof(Get_Check_For_Valid_Index_Params))]
        public void Check_Valid_Index(int bufferSize, string searchString, string input, long expectedIndex)
        {
            var inputStream = input.ToMemoryStream();
            var searchSequence = searchString.ToArray();

            var searchStream = new SearchStream(inputStream, searchSequence);

            searchStream.Drain(bufferSize: bufferSize);

            Assert.NotNull(searchStream.Result);
            Assert.Equal(expectedIndex, searchStream.Result!.OffsetFound);
        }

        public static IEnumerable<object[]> Get_Check_For_Valid_Index_Params()
        {
            var baseTestCases = new List<object[]>()
            {
                new object[] {  "de", "abcdef", 3 },
                new object[] {  "abc", "abcdef", 0 },
                new object[] {  "f", "abcdef", 5 },
                new object[] { "jklmn", "abcdefghijklmnopqrstuvwxyz", 9 },
                new object[] { "awxyz", "abcdefghijklmnopqrstuvwxyz", -1 },
                new object[] { "vwxyz", "abcdefghijklmnopqrstuvwxyz", 21 },
            };

            var bufferSizes = new int[] { 1,14,5,4,20 };

            foreach (var buffersize in bufferSizes)
            {
                foreach (var baseTestCase in baseTestCases)
                {
                    yield return new List<object>() { buffersize }.Concat(baseTestCase).ToArray();
                }
            }
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
