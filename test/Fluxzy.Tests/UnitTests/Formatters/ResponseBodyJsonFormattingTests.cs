// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text.Json;
using Fluxzy.Formatters.Producers.Responses;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Formatters
{
    public class ResponseBodyJsonFormattingTests
    {
        [Fact]
        public void Decode_Unicode_Escapes()
        {
            var result = ResponseBodyJsonProducer.Format(
                "{\"hebrew\":\"\\u05E9\\u05DC\\u05D5\\u05DD\"}");

            Assert.Contains("\"שלום\"", result);
        }

        [Fact]
        public void Combine_Surrogate_Pairs()
        {
            var result = ResponseBodyJsonProducer.Format("{\"emoji\":\"\\uD83D\\uDE00\"}");

            Assert.Contains("\"😀\"", result);
        }

        [Fact]
        public void Combine_Consecutive_Surrogate_Pairs()
        {
            var result = ResponseBodyJsonProducer.Format(
                "{\"emoji\":\"\\uD83D\\uDE00\\uD83D\\uDE01\"}");

            Assert.Contains("\"😀😁\"", result);
        }

        [Fact]
        public void Keep_Raw_Non_Ascii_Readable()
        {
            var result = ResponseBodyJsonProducer.Format("{\"hebrew\":\"שלום\",\"emoji\":\"😀\"}");

            Assert.Contains("\"שלום\"", result);
            Assert.Contains("\"😀\"", result);
        }

        [Fact]
        public void Decode_Escaped_Html_Characters()
        {
            var result = ResponseBodyJsonProducer.Format(
                "{\"html\":\"\\u003Cspan class=\\u0022em\\u0022\\u003E\\u05E9\\u003C/span\\u003E\"}");

            Assert.Contains("\"<span class=\\\"em\\\">ש</span>\"", result);
        }

        [Fact]
        public void Preserve_Escaped_Backslash_Literal()
        {
            var result = ResponseBodyJsonProducer.Format("{\"literal\":\"\\\\u05E9\"}");

            Assert.Contains("\"\\\\u05E9\"", result);
        }

        [Fact]
        public void Preserve_Escaped_Backslash_Before_Surrogate_Digits()
        {
            var result = ResponseBodyJsonProducer.Format("{\"literal\":\"\\\\uD83D\\\\uDE00\"}");

            Assert.Contains("\"\\\\uD83D\\\\uDE00\"", result);
            Assert.DoesNotContain("😀", result);
        }

        [Fact]
        public void Combine_Surrogate_Pair_After_Literal_Backslash()
        {
            var result = ResponseBodyJsonProducer.Format("{\"mixed\":\"\\\\\\uD83D\\uDE00\"}");

            Assert.Contains("\"\\\\😀\"", result);
        }

        [Fact]
        public void Keep_Control_Characters_Escaped()
        {
            var result = ResponseBodyJsonProducer.Format("{\"control\":\"\\u0001\"}");

            Assert.Contains("\\u0001", result, StringComparison.Ordinal);
            Assert.DoesNotContain("\u0001", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Preserve_Large_Number_Literals()
        {
            var result = ResponseBodyJsonProducer.Format("{\"large\":9007199254740993123456789}");

            Assert.Contains("9007199254740993123456789", result);
        }

        [Fact]
        public void Preserve_Duplicate_Keys_And_Order()
        {
            var result = ResponseBodyJsonProducer.Format("{\"dup\":1,\"dup\":2}");

            var firstIndex = result.IndexOf("\"dup\": 1", StringComparison.Ordinal);
            var secondIndex = result.IndexOf("\"dup\": 2", StringComparison.Ordinal);

            Assert.True(firstIndex >= 0);
            Assert.True(secondIndex > firstIndex);
        }

        [Fact]
        public void Output_Remains_Valid_Json()
        {
            var result = ResponseBodyJsonProducer.Format(
                "{\"hebrew\":\"\\u05E9\",\"emoji\":\"\\uD83D\\uDE00\",\"literal\":\"\\\\u05E9\",\"quote\":\"\\\"\"}");

            using var document = JsonDocument.Parse(result);

            Assert.Equal("ש", document.RootElement.GetProperty("hebrew").GetString());
            Assert.Equal("😀", document.RootElement.GetProperty("emoji").GetString());
            Assert.Equal("\\u05E9", document.RootElement.GetProperty("literal").GetString());
            Assert.Equal("\"", document.RootElement.GetProperty("quote").GetString());
        }
    }
}
