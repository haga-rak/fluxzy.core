using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core.Pcap;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Pcap
{
    public class IpUtilityTests
    {
        [Fact]
        public void DefaultRoute()
        {
            var firstAddr = IpUtility.GetDefaultRouteV4Address();
            var secondAddr = IpUtility.GetDefaultRouteV4Address();

            Assert.True(object.ReferenceEquals(firstAddr, secondAddr));
        }

        [Fact]
        public void GetFreeEndPoint()
        {
            var endPoint = IpUtility.GetFreeEndpoint();

            Assert.True(endPoint.Port > 0);
        }

        [Fact]
        public void CaptureAvailabilityChecker_Available()
        {
            var available = CaptureAvailabilityChecker.Instance.IsAvailable;

            Assert.True(available);
        }
    }
}
