// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Text;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class BinaryMatcherTests
    {
        [Theory]
        [MemberData(nameof(GenerateArgsForTestStrings))]
        public void TestStrings(string encodingName, string contentString, string patternString, int expectedIndex)
        {
            // Arrange
            var encoding = Encoding.GetEncoding(encodingName);
            var content = contentString.ToBytes(encoding);
            var pattern = patternString.ToBytes(encoding);
            var matcher = new StringBinaryMatcher(encoding); 

            // Act
            var index = matcher.FindIndex(content, pattern);

            // Assert
            Assert.Equal(expectedIndex, index);
        }

        public static IEnumerable<object[]> GenerateArgsForTestStrings()
        {
            var encoding = new[] { Encoding.UTF8, Encoding.ASCII };

            var stringTestCases = new List<TestStringsArgsTuple>() {
                new("abcd", "bc", 1),
                new("abcd", "d", 3),
                new("abcd", "cd", 2),
                new("abcd", "cdefghijklmn", -1),
                new("abcd", "a", 0),
                new("abcd", "", 0),
                new("", "", 0),
                new("", "sdf", -1),
                new("ab👉😘cd", "👉😘", 2),
            }; 
            
            foreach (var enc in encoding)
            {
                foreach (var testCase in stringTestCases)
                {
                    yield return new object[] { enc.WebName, testCase.Content, testCase.Pattern, testCase.ExpectedIndex};
                }
            }
        }

        internal record TestStringsArgsTuple(string Content, string Pattern, int ExpectedIndex)
        {
            
        }
    }
        
    internal static class ToBytesExtension
    {
        public static byte[] ToBytes(this string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }
    }
}
