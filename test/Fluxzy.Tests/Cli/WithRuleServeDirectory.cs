// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleServeDirectory : WithRuleOptionBase
    {
        [Theory]
        [InlineData("index.html", 214010, "text/html", 200)]
        [InlineData("", 214010, "text/html", 200)]
        [InlineData("index_files/googlePlay.png", 6271, "image/png", 200)]
        [InlineData("/index_files/googlePlay.png", 6271, "image/png", 200)]
        [InlineData("../../yes.html", 0, null, 404)]
        public async Task Validate_Serve_Static_Dir(string path, long size, string? mime, int statusCode)
        {
            // Arrange
            var yamlContent = $"""
                rules:
                - filter: 
                    typeKind: anyFilter  
                  action : 
                    typeKind: serveDirectoryAction
                    directory: '{Startup.DirectoryName}'
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.google.com/{path}");

            // Act

            using var response = await Exec(yamlContent, requestMessage);
                
            var responseBodyBytes = await response.Content.ReadAsByteArrayAsync();
            
            var headers = response.Content.Headers;

            // Assert

            Assert.Equal(statusCode, (int) response.StatusCode);

            if (response.IsSuccessStatusCode) {
                Assert.Equal(size, responseBodyBytes.Length);
                Assert.True(headers.TryGetValues("content-type", out var contentTypeValues));
                Assert.Equal(mime, contentTypeValues.First());
            }
        }
    }
}
