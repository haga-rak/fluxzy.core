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
        public void TestStrings(Encoding encoding, byte [] content, byte [] pattern, int expectedIndex)
        {
            var matcher = new StringBinaryMatcher(encoding); 
            var index = matcher.FindIndex(content, pattern);
            Assert.Equal(expectedIndex, index);
        }

        public static IEnumerable<object[]> GenerateArgsForTestStrings()
        {
            var encoding = new[] { Encoding.UTF8, Encoding.ASCII };

            var stringTestCases = new List<TestStringsArgsTuple>() {
                new("abcd", "bc", 1),
                new("abcd", "d", 3),
                new("abcd", "cd", 2),
                new("abcd", "", 0),
                new("", "", 0),
                new("", "sdf", -1),
                new("ab👉😘cd", "👉😘", 2),
            }; 
            
            foreach (var enc in encoding)
            {
                foreach (var testCase in stringTestCases)
                {
                    yield return new object[] { enc, testCase.Content.ToBytes(enc), testCase.Pattern.ToBytes(enc), testCase.ExpectedIndex};
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
