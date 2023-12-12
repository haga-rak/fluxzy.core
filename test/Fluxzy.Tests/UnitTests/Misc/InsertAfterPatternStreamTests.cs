// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Text;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class InsertAfterPatternStreamTests
    {
        [Theory]
        [InlineData("Hello World", "World", "!", "Hello World!")]
        [InlineData("Hello World", "W", "!", "Hello W!orld")]
        [InlineData("123abcd987", "abcd", "ççç", "123abcdççç987")]
        public void TestWithContent(string content, string pattern, string insertedText, string expected)
        {
            var matcher = new StringBinaryMatcher(Encoding.UTF8);
            var matchingPattern = pattern.ToBytes(Encoding.UTF8);
            var contentStream = content.ToUtf8Stream();
            var insertedTextStream = insertedText.ToUtf8Stream(); 
            
            var stream = new InsertAfterPatternStream(contentStream, matcher, matchingPattern, insertedTextStream);

            var result = stream.ReadToEndWithCustomBuffer(bufferSize: 1); 

            Assert.Equal(expected, result);
        }
    }

    internal static class BinaryHelper
    {
        public static Stream ToUtf8Stream(this string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }
    }
}
