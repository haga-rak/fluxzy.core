// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Fluxzy.Core.Pcap.Pcapng;
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

        [Fact]
        public void Validate_With_Fix_Hash()
        {
            var directory = ".artefacts/tests/pink-floyd/captures";
            var outFile = ".artefacts/tests/__full.pcapng";

            var pcapngFiles = new DirectoryInfo(directory).EnumerateFiles("*.pcapng").ToList();
            var nssKeys = new DirectoryInfo(directory).EnumerateFiles("*.nsskeylog").ToList();

            using (var outStream = File.Create(outFile)) {
                PcapMerge.Merge(pcapngFiles, nssKeys, outStream);
            }

            Assert.True(File.Exists(outFile));
            Assert.Equal(572384, new FileInfo(outFile).Length);
            Assert.Equal("f208adf9a27f2cc2d4b88a19d89658f1ef6b3c6acaa96c3206db2b74bfb8a080", HashHelper.MakeWinGetHash(outFile));
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
