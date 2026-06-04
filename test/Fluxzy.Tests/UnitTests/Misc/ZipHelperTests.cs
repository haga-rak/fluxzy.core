// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    /// <summary>
    ///     Covers the migration of <c>ZipHelper</c> from SharpZipLib to <see cref="System.IO.Compression" />:
    ///     directory round-trip, pcap files stored uncompressed, and zip-slip protection on extraction.
    /// </summary>
    public class ZipHelperTests : IDisposable
    {
        private readonly string _root =
            Path.Combine(Path.GetTempPath(), "fluxzy-ziphelper-tests", Guid.NewGuid().ToString("N"));

        [Fact]
        public async Task Compress_then_decompress_roundtrips_files_and_subdirectories()
        {
            var source = NewDir("src");
            File.WriteAllText(Path.Combine(source, "a.txt"), "hello A");

            var subDir = Path.Combine(source, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "b.txt"), "hello B nested");

            using var ms = new MemoryStream();
            await ZipHelper.Compress(new DirectoryInfo(source), ms, _ => true);

            ms.Position = 0;
            var target = NewDir("dst");
            await ZipHelper.DecompressAsync(ms, new DirectoryInfo(target));

            Assert.Equal("hello A", File.ReadAllText(Path.Combine(target, "a.txt")));
            Assert.Equal("hello B nested", File.ReadAllText(Path.Combine(target, "sub", "b.txt")));
        }

        [Fact]
        public async Task Sync_decompress_roundtrips()
        {
            var source = NewDir("src-sync");
            File.WriteAllText(Path.Combine(source, "a.txt"), "sync content");

            using var ms = new MemoryStream();
            await ZipHelper.Compress(new DirectoryInfo(source), ms, _ => true);

            ms.Position = 0;
            var target = NewDir("dst-sync");
            ZipHelper.Decompress(ms, new DirectoryInfo(target));

            Assert.Equal("sync content", File.ReadAllText(Path.Combine(target, "a.txt")));
        }

        [Fact]
        public async Task Pcap_files_are_stored_uncompressed()
        {
            var source = NewDir("src");
            var compressible = new string('A', 8192);

            File.WriteAllText(Path.Combine(source, "capture.pcapng"), compressible);
            File.WriteAllText(Path.Combine(source, "data.txt"), compressible);

            using var ms = new MemoryStream();
            await ZipHelper.Compress(new DirectoryInfo(source), ms, _ => true);

            ms.Position = 0;
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

            var pcap = archive.Entries.Single(e => e.Name == "capture.pcapng");
            var txt = archive.Entries.Single(e => e.Name == "data.txt");

            Assert.Equal(pcap.Length, pcap.CompressedLength); // stored (NoCompression)
            Assert.True(txt.CompressedLength < txt.Length);    // compressed
        }

        [Fact]
        public void Decompress_rejects_zip_slip_entries()
        {
            using var ms = new MemoryStream();

            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true)) {
                var entry = archive.CreateEntry("../escaped.txt");
                using var s = entry.Open();
                s.Write(Encoding.UTF8.GetBytes("pwned"));
            }

            ms.Position = 0;

            var target = NewDir("dst-slip");
            ZipHelper.Decompress(ms, new DirectoryInfo(target));

            var escaped = Path.GetFullPath(Path.Combine(target, "..", "escaped.txt"));

            Assert.False(File.Exists(escaped),
                "zip-slip entry must not be written outside the target directory");
        }

        [Fact]
        public async Task CompressWithFileInfos_roundtrips()
        {
            var source = NewDir("src-fi");
            var file = Path.Combine(source, "x.json");
            File.WriteAllText(file, "{\"k\":1}");

            byte[] bytes;

            // CompressWithFileInfos owns/closes the output stream, so capture the buffer afterwards.
            using (var ms = new MemoryStream()) {
                await ZipHelper.CompressWithFileInfos(new DirectoryInfo(source), ms,
                    new[] { new FileInfo(file) });

                bytes = ms.ToArray();
            }

            var target = NewDir("dst-fi");

            using (var input = new MemoryStream(bytes)) {
                await ZipHelper.DecompressAsync(input, new DirectoryInfo(target));
            }

            Assert.Equal("{\"k\":1}", File.ReadAllText(Path.Combine(target, "x.json")));
        }

        private string NewDir(string name)
        {
            var path = Path.Combine(_root, name);
            Directory.CreateDirectory(path);

            return path;
        }

        public void Dispose()
        {
            try {
                if (Directory.Exists(_root))
                    Directory.Delete(_root, true);
            }
            catch {
                // best-effort cleanup
            }
        }
    }
}
