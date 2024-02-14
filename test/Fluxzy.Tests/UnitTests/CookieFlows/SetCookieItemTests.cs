// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Formatters.Producers.Responses;
using Xunit;

namespace Fluxzy.Tests.UnitTests.CookieFlows
{
    public class SetCookieItemTests
    {
        [Theory]
        [InlineData("abc=123; Expires=Wed, 09 Jun 2021 10:18:14 GMT; Path=/; Domain=.httpbin.org; Secure; HttpOnly", "abc", "123", true)]
        [InlineData("freeform=; Expires=Thu, 01-Jan-1970 00:00:00 GMT; Max-Age=0; Path=/", "freeform", "", true)]
        public void SimpleParsing(string rawValue, string name, string value, bool success)
        {
            var result = SetCookieItem.TryParse(rawValue, out var item);

            Assert.Equal(success, result);

            if (success) {
                Assert.Equal(name, item!.Name);
                Assert.Equal(value, item!.Value);
            }
        }
    }
}
