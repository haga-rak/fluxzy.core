// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients.Mock;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class MockedResponseContentTests
    {
        [Fact]
        public void CreateEmptyWithStatusCode()
        {
            var result = MockedResponseContent.CreateEmptyWithStatusCode(200);

            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public void CreateFromEmptyByteArray()
        {
            var result = MockedResponseContent.CreateFromByteArray(Array.Empty<byte>(), 200, "text/plain");

            Assert.Equal(200, result.StatusCode);
            ;
            ValidateBody(result);
            Assert.NotNull(result.Body!.Content);
            Assert.Equal(0, result.Body.Content.Length);

            Assert.Contains("text", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromByteArray()
        {
            var result = MockedResponseContent.CreateFromByteArray(new byte[4], 200, "text/plain");

            Assert.Equal(200, result.StatusCode);
            ;
            ValidateBody(result);
            Assert.NotNull(result.Body!.Content);
            Assert.Equal(4, result.Body.Content.Length);

            Assert.Contains("text", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromFile()
        {
            var result = MockedResponseContent.CreateFromFile("_Files/Archives/test.data");

            Assert.Equal(200, result.StatusCode);
            ValidateBody(result);


            Assert.Contains("octet-stream", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromHtmlContent()
        {
            var result = MockedResponseContent.CreateFromHtmlContent("_Files/Archives/test.json");

            Assert.Equal(200, result.StatusCode);
            ValidateBody(result);

            Assert.Contains("html", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromHtmlFile()
        {
            var result = MockedResponseContent.CreateFromHtmlFile("_Files/Archives/test.json");

            Assert.Equal(200, result.StatusCode);
            ValidateBody(result);

            Assert.Contains("html", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromJsonContent()
        {
            var result = MockedResponseContent.CreateFromJsonContent("{ x : 5 }");

            Assert.Equal(200, result.StatusCode);
            ValidateBody(result);

            Assert.Contains("json", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromJsonFile()
        {
            var result = MockedResponseContent.CreateFromJsonFile("_Files/Archives/test.json");

            Assert.Equal(200, result.StatusCode);
            ValidateBody(result);

            Assert.Contains("json", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromPlainText()
        {
            var result = MockedResponseContent.CreateFromPlainText("mytext");

            Assert.Equal(200, result.StatusCode);
            ValidateBody(result);

            Assert.Contains("text", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromPlainTextFile()
        {
            var result = MockedResponseContent.CreateFromPlainTextFile("_Files/Archives/test.json");

            Assert.Equal(200, result.StatusCode);
            ValidateBody(result);

            Assert.Contains("text", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromString()
        {
            var result = MockedResponseContent.CreateFromString("_Files/Archives/test.json", 202, "text/plain");

            Assert.Equal(202, result.StatusCode);
            ValidateBody(result);

            Assert.Contains("text", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromEmptyString()
        {
            var result = MockedResponseContent.CreateFromString("", 202, "text/plain");

            Assert.Equal(202, result.StatusCode);
            ValidateBody(result);

            Assert.Contains("text", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CreateFromXmlContent()
        {
            var result = MockedResponseContent.CreateFromXmlContent("<root/>");

            Assert.Equal(200, result.StatusCode);
            ValidateBody(result);

            Assert.Contains("xml", result.GetHeaderValueOrDefault("content-type"),
                StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateBody(MockedResponseContent result)
        {
            Assert.NotNull(result.Body);

            switch (result.Body.Origin) {
                case BodyContentLoadingType.FromFile:
                    Assert.NotNull(result.Body.FileName);

                    break;

                case BodyContentLoadingType.FromImmediateArray:
                    Assert.NotNull(result.Body.Content);

                    break;

                case BodyContentLoadingType.FromString:
                    Assert.NotNull(result.Body.Text);

                    break;

                case BodyContentLoadingType.NotSet:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
