// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class InjectStreamOnStreamTests
    {
        [Theory]
        [MemberData(nameof(GenerateArgsForInsertAfter))]
        public void Test_Insert_After(string content, string pattern, string insertedText, string?  expected, int bufferSize)
        {
            var matcher = new InsertAfterBinaryMatcher(Encoding.UTF8);
            var matchingPattern = pattern.ToBytes(Encoding.UTF8);
            var contentStream = content.ToUtf8Stream();
            var insertedTextStream = insertedText.ToUtf8Stream();

            expected ??= content;  // if expected is null, we expect the same content

            var stream = new InjectStreamOnStream(contentStream, matcher, matchingPattern, insertedTextStream);

            var result = stream.ReadToEndWithCustomBuffer(bufferSize: bufferSize); 

            Assert.Equal(expected, result);
        }
        
        [Theory]
        [MemberData(nameof(GenerateArgsForReplace))]
        public void Test_Replace(string content, string pattern, string insertedText, string?  expected, int bufferSize)
        {
            var matcher = new ReplaceBinaryMatcher(Encoding.UTF8);
            var matchingPattern = pattern.ToBytes(Encoding.UTF8);
            var contentStream = content.ToUtf8Stream();
            var insertedTextStream = insertedText.ToUtf8Stream();

            expected ??= content;  // if expected is null, we expect the same content

            var stream = new InjectStreamOnStream(contentStream, matcher, matchingPattern, insertedTextStream);

            var result = stream.ReadToEndWithCustomBuffer(bufferSize: bufferSize); 

            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(GenerateArgsForInsertDetectHtml))]
        public void Test_Detect_Html_Insert(string content, string pattern, string insertedText, string?  expected, int bufferSize)
        {
            var matcher = new SimpleHtmlTagOpeningMatcher(Encoding.UTF8, StringComparison.OrdinalIgnoreCase, false);
            var matchingPattern = pattern.ToBytes(Encoding.UTF8);
            var contentStream = content.ToUtf8Stream();
            var insertedTextStream = insertedText.ToUtf8Stream();

            expected ??= content;  // if expected is null, we expect the same content

            var stream = new InjectStreamOnStream(contentStream, matcher, matchingPattern, insertedTextStream);

            var result = stream.ReadToEndWithCustomBuffer(bufferSize: bufferSize); 

            Assert.Equal(expected, result, StringComparer.OrdinalIgnoreCase);
        }

        public record TestContentArgsTuple(string Content, string Pattern, string InsertedText, string? Expected);

        public static TheoryData<string, string, string, string?, int> GenerateArgsForInsertAfter()
        {
            var tuples = new TestContentArgsTuple[] {
                new("Hello World", "World", "!", "Hello World!"),
                new("Hello World", "W", "!", "Hello W!orld"),
                new("123abcd987", "abcd", "ççç", "123abcdççç987"),
                new("123abcd987", "", "ççç", "ççç123abcd987"),
                new("123abcd987", "xxxx", "not_run", null),
                new("", "xxxx", "not_run", null),
            };

            var bufferSize = new[] { 1, 3,1024 }; // 1 buffer for testing internal loop

            var data = new TheoryData<string, string, string, string?, int>();

            foreach (var tuple in tuples) {
                foreach (var size in bufferSize) {
                    data.Add(tuple.Content, tuple.Pattern, tuple.InsertedText, tuple.Expected, size);
                }
            }

            return data;
        }

        public static TheoryData<string, string, string, string?, int> GenerateArgsForReplace()
        {
            var tuples = new TestContentArgsTuple[] {
                new("Hello World", "World", "!", "Hello !"),
                new("Hello World", "W", "!", "Hello !orld"),
                new("123abcd987", "abcd", "ççç", "123ççç987"),
                new("123abcd987", "", "ççç", "ççç123abcd987"),
                new("123abcd987", "xxxx", "not_run", null),
                new("", "xxxx", "not_run", null),
            };

            var bufferSize = new[] { 1, 3, 1024 }; // 1 buffer for testing internal loop

            var data = new TheoryData<string, string, string, string?, int>();

            foreach (var tuple in tuples) {
                foreach (var size in bufferSize) {
                    data.Add(tuple.Content, tuple.Pattern, tuple.InsertedText, tuple.Expected, size);
                }
            }

            return data;
        }

        public static TheoryData<string, string, string, string?, int> GenerateArgsForInsertDetectHtml()
        {
            var tuples = new TestContentArgsTuple[] {
                new("<html><head><title>", "head", "!", "<html><head>!<title>"),
                new("<html><  head><title>", "head", "!", "<html><  head>!<title>"),
                new("<html><  head  ><title>", "head", "!", "<html><  head  >!<title>"),
                new("<html><  hEad  ><title>", "head", "!", "<html><  heAd  >!<title>"),
            };

            var bufferSize = new[] { 1, 3, 1024 }; // 1 buffer for testing internal loop

            var data = new TheoryData<string, string, string, string?, int>();

            foreach (var tuple in tuples) {
                foreach (var size in bufferSize) {
                    data.Add(tuple.Content, tuple.Pattern, tuple.InsertedText, tuple.Expected, size);
                }
            }

            return data;
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
