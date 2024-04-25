// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text;
using Fluxzy.Formatters.Producers.Requests;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class HttpHelperTests
    {
        [Theory]
        [InlineData("HTTP/1.1 100 Continue\r\n\r\n", true)]
        [InlineData("HTTP/1.0 100 Continue\r\n\r\n", true)]
        [InlineData("HTTP/1.1 101 Continue\r\n", false)]
        [InlineData("HTTP", false)]
        [InlineData("", false)]
        public void Is100Continue(string header, bool success)
        {
            var headerBytes = Encoding.UTF8.GetBytes(header);

            var result = HttpHelper.Is100Continue(headerBytes);

            Assert.Equal(success, result);
        }
    }
}
