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
            var outFile = GetRegisteredRandomFile();

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
            var outFile = GetRegisteredRandomFile();

            using (var outStream = File.Create(outFile)) {
                PcapMerge.MergeDumpDirectory(directory, outStream);
            }

            Assert.True(File.Exists(outFile));
            Assert.Equal(572384, new FileInfo(outFile).Length); 
            Assert.Equal("5cf21b2c48ae241f46ddebf30fca6de2f757df52d00d9cf939b750f0296d33b8", HashHelper.MakeWinGetHash(outFile));
        }

        [Fact]
        public void Validate_With_Archive()
        {
            var archiveFile = "_Files/Archives/pink-floyd.fxzy";
            var outFile = GetRegisteredRandomFile();

            using (var outStream = File.Create(outFile)) {
                PcapMerge.MergeArchive(archiveFile, outStream);
            }

            Assert.True(File.Exists(outFile));
            Assert.Equal(572384, new FileInfo(outFile).Length); 
            Assert.Equal("5cf21b2c48ae241f46ddebf30fca6de2f757df52d00d9cf939b750f0296d33b8", HashHelper.MakeWinGetHash(outFile));
        }

        [Fact]
        public void Validate_With_Dump_Directory_31_Only()
        {
            var hash = "5aedc27fc85ada8b5c224679eeb83d2f3ad75dcac4caa98aae6a927d49d96ab8";
            var directory = ".artefacts/tests/pink-floyd";
            var outFile = GetRegisteredRandomFile();

            using (var outStream = File.Create(outFile)) {
                PcapMerge.MergeDumpDirectory(directory, outStream, connectionIds: new () { 31 });
            }

            Assert.True(File.Exists(outFile));
            Assert.Equal(hash, 
                HashHelper.MakeWinGetHash(outFile));
        }

        [Fact]
        public void Validate_With_Dump_Directory_Filtered_Connection()
        {
            var directory = ".artefacts/tests/pink-floyd";
            var outFile = GetRegisteredRandomFile();

            using (var outStream = File.Create(outFile)) {
                PcapMerge.MergeDumpDirectory(directory, outStream);
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
