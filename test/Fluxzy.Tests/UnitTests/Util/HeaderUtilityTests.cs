// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Utils;
using NSubstitute;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class HeaderUtilityTests
    {
        [Theory]
        [InlineData("application/json", "json")]
        [InlineData("application/xml", "xml")]
        [InlineData("text/html", "html")]
        [InlineData("text/plain", "txt")]
        [InlineData("application/javascript", "js")]
        [InlineData("application/pdf", "pdf")]
        [InlineData("text/font", "font")]
        [InlineData("text/css", "css")]
        [InlineData("img/jpg", "jpeg")]
        [InlineData("img/jpeg", "jpeg")]
        [InlineData("img/png", "png")]
        [InlineData("img/gif", "gif")]
        [InlineData("img/svg", "svg")]
        [InlineData("unknown/unknown", "data")]
        public void GetRequestSuggestedExtension(string contentType, string result)
        {
            var exchange = Substitute.For<IExchange>();

            exchange.GetRequestHeaders().Returns(
                new[] {
                    new HeaderFieldInfo("Content-Type", contentType)
                }
            );

            var suggestion = HeaderUtility.GetRequestSuggestedExtension(exchange);

            Assert.Equal(result, suggestion);
        }

        [Theory]
        [InlineData("application/json", "json")]
        [InlineData("application/xml", "xml")]
        [InlineData("text/html", "html")]
        [InlineData("text/plain", "txt")]
        [InlineData("application/javascript", "js")]
        [InlineData("application/pdf", "pdf")]
        [InlineData("text/font", "font")]
        [InlineData("text/css", "css")]
        [InlineData("img/jpg", "jpeg")]
        [InlineData("img/jpeg", "jpeg")]
        [InlineData("img/png", "png")]
        [InlineData("img/gif", "gif")]
        [InlineData("img/svg", "svg")]
        [InlineData("unknown/unknown", "data")]
        public void GetResponseSuggestedExtension(string contentType, string result)
        {
            var exchange = Substitute.For<IExchange>();

            exchange.GetResponseHeaders().Returns(
                new[] {
                    new HeaderFieldInfo("Content-Type", contentType)
                }
            );

            var suggestion = HeaderUtility.GetResponseSuggestedExtension(exchange);

            Assert.Equal(result, suggestion);
        }

        [Theory]
        [InlineData("application/json", "json")]
        [InlineData("application/xml", "xml")]
        [InlineData("text/html", "html")]
        [InlineData("text/plain", "text")]
        [InlineData("application/javascript", "js")]
        [InlineData("application/pdf", "pdf")]
        [InlineData("text/font", "font")]
        [InlineData("text/css", "css")]
        [InlineData("image/jpg", "img")]
        [InlineData("image/jpeg", "img")]
        [InlineData("image/png", "img")]
        [InlineData("image/gif", "img")]
        [InlineData("image/svg", "img")]
        [InlineData("application/octet-stream", "bin")]
        public void GetSimplifiedContentType(string contentType, string result)
        {
            var exchange = Substitute.For<IExchange>();

            exchange.GetResponseHeaders().Returns(
                new[] {
                    new HeaderFieldInfo("Content-Type", contentType)
                }
            );

            var suggestion = HeaderUtility.GetSimplifiedContentType(exchange);

            Assert.Equal(result, suggestion);
        }
    }
}
