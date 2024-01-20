// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Fluxzy.Core.Pcap.Pcapng.Merge;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Pcap.Merge
{
    public class PcapMergeTests : ProduceDeletableItem
    {
        [Fact]
        public void Validate_3()
        {
            var directory = ".artefacts/tests/pink-floyd/captures";
            var outFile = ".artefacts/tests/__full3.pcapng";

            var pcapngFiles = new DirectoryInfo(directory).EnumerateFiles("3*.pcapng").ToList();
            var nssKeys = new DirectoryInfo(directory).EnumerateFiles("*.nsskeylog").ToList();

            using (var outStream = File.Create(outFile)) {
                PcapMerge.Merge(pcapngFiles, nssKeys, outStream);
            }

            Assert.True(File.Exists(outFile));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public void Validate_With_Fix_Hash(int concurrentFileOpen)
        {
            var directory = ".artefacts/tests/pink-floyd/captures";
            var outFile = ".artefacts/tests/__full.pcapng";

            var pcapngFiles = new DirectoryInfo(directory).EnumerateFiles("*.pcapng").ToList();
            var nssKeys = new DirectoryInfo(directory).EnumerateFiles("*.nsskeylog").ToList();

            using (var outStream = File.Create(outFile)) {
                PcapMerge.Merge(pcapngFiles, nssKeys, outStream,
                    maxConcurrentOpenFile: concurrentFileOpen);
            }

            Assert.True(File.Exists(outFile));
            Assert.Equal(572384, new FileInfo(outFile).Length); 
            Assert.Equal("5cf21b2c48ae241f46ddebf30fca6de2f757df52d00d9cf939b750f0296d33b8", HashHelper.MakeWinGetHash(outFile));
        }

        [Fact]
        public void Validate_With_Dump_Directory()
        {
            var directory = ".artefacts/tests/pink-floyd";
            var outFile = ".artefacts/tests/__full.pcapng";

            using (var outStream = File.Create(outFile)) {
                PcapMerge.Merge(directory, outStream);
            }

            Assert.True(File.Exists(outFile));
            Assert.Equal(572384, new FileInfo(outFile).Length); 
            Assert.Equal("5cf21b2c48ae241f46ddebf30fca6de2f757df52d00d9cf939b750f0296d33b8", HashHelper.MakeWinGetHash(outFile));
        }

        [Theory]
        [InlineData("14.pcapng", "*.nsskeylog")]
        [InlineData("14.pcapng", "undefined")]
        public void Validate(string pcapPattern, string nssPattern)
        {
            var directory = ".artefacts/tests/pink-floyd/captures";
            var outFile = GetRegisteredRandomFile();

            var pcapngFiles = new DirectoryInfo(directory).EnumerateFiles(pcapPattern).ToList();
            var nssKeys = new DirectoryInfo(directory).EnumerateFiles(nssPattern).ToList();

            using (var outStream = File.Create(outFile)) {
                PcapMerge.Merge(pcapngFiles, nssKeys, outStream);
            }

            Assert.True(File.Exists(outFile));
            Assert.True(new FileInfo(outFile).Length > 0);
        }
    }

    internal static class HashHelper
    {
        public static string MakeWinGetHash(string fileName)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(fileName); 
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
