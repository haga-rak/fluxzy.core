// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using Fluxzy.Core.Pcap;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Pcap
{
    public class IpAddressHashHelperTests
    {
        [Theory]
        [InlineData("192.16.5.6", "192.16.5.6", true)]
        [InlineData("192.16.5.6", "192.16.5.1", false)]
        [InlineData("92.222.9.112", "::ffff:92.222.9.112", true)]
        [InlineData("1050:0:0:0:5:600:300c:326b", "1050:0:0:0:5:600:300c:326b", true)]
        [InlineData("1050:0:0:0:5:600:300d:326b", "1050:0:0:0:5:600:300c:326b", false)]
        public void Validate(string inputA, string inputB, bool equal)
        {
            var ipAddressA = IPAddress.Parse(inputA);
            var ipAddressB = IPAddress.Parse(inputB);
            
            var hashA = ipAddressA.Get4BytesHash();
            var hashB = ipAddressB.Get4BytesHash();

            if (equal)
                Assert.Equal(hashA, hashB);
            else
                Assert.NotEqual(hashA, hashB);
        }
    }
}
