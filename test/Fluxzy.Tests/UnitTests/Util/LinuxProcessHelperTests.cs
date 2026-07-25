// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fluxzy.Utils.ProcessTracking;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class LinuxProcessHelperTests
    {
        private const string Header =
            "  sl  local_address rem_address   st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode";

        private static HashSet<long> Collect(int localPort, params string[] lines)
        {
            var content = string.Join("\n", new[] { Header }.Concat(lines));
            var result = new HashSet<long>();

            LinuxProcessHelper.CollectSocketInodes(new StringReader(content), localPort, result);

            return result;
        }

        [Fact]
        public void CollectSocketInodes_EstablishedSocket()
        {
            // 0xA1B2 == 41394
            var inodes = Collect(41394,
                "   0: 0100007F:A1B2 0100007F:1F90 01 00000000:00000000 00:00000000 00000000  1000        0 987654 1 0000000000000000 20 0 0 10 -1");

            Assert.Equal(new HashSet<long> { 987654 }, inodes);
        }

        [Fact]
        public void CollectSocketInodes_SkipsTimeWaitShadowingLivePort()
        {
            // A TIME_WAIT entry (st 06, inode 0) may hold the same local port as a live socket:
            // the ephemeral port gets reused for a different remote tuple. The live inode must win.
            var inodes = Collect(41394,
                "   0: 0100007F:A1B2 0100007F:2710 06 00000000:00000000 00:00000000 00000000     0        0 0 0 0000000000000000",
                "   1: 0100007F:A1B2 0100007F:1F90 01 00000000:00000000 00:00000000 00000000  1000        0 987654 1 0000000000000000 20 0 0 10 -1");

            Assert.Equal(new HashSet<long> { 987654 }, inodes);
        }

        [Fact]
        public void CollectSocketInodes_IgnoresRemotePortMatches()
        {
            var inodes = Collect(41394,
                "   0: 0100007F:1F90 0100007F:A1B2 01 00000000:00000000 00:00000000 00000000  1000        0 987654 1 0000000000000000 20 0 0 10 -1");

            Assert.Empty(inodes);
        }

        [Fact]
        public void CollectSocketInodes_CollectsEveryCandidate()
        {
            var inodes = Collect(41394,
                "   0: 0100007F:A1B2 00000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 111 1 0000000000000000 100 0 0 10 0",
                "   1: 0100007F:A1B2 0100007F:1F90 01 00000000:00000000 00:00000000 00000000  1000        0 222 1 0000000000000000 20 0 0 10 -1");

            Assert.Equal(new HashSet<long> { 111, 222 }, inodes);
        }

        [Fact]
        public void CollectSocketInodes_ParsesIpV6Table()
        {
            var inodes = Collect(41394,
                "   0: 00000000000000000000000001000000:A1B2 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 333 1 0000000000000000 100 0 0 10 0");

            Assert.Equal(new HashSet<long> { 333 }, inodes);
        }
    }
}
