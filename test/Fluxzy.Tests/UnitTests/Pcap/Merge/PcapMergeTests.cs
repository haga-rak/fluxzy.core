// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using Fluxzy.Core.Pcap.Pcapng;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Pcap.Merge
{
    public class PcapMergeTests
    {
        [Fact]
        public void Test()
        {
            var directory = ".artefacts/tests/pink-floyd/captures";
            var outFile = ".artefacts/tests/__full.pcapng";

            var pcapngFiles = new DirectoryInfo(directory).EnumerateFiles("14.pcapng").ToList();
            var nssKeys = new DirectoryInfo(directory).EnumerateFiles("*.nsskeylog").ToList();

            using var outStream = File.Create(outFile); 

            PcapMerge.Merge(pcapngFiles, Array.Empty<FileInfo>(), outStream);
        }
    }
}
