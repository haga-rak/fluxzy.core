using Fluxzy.Tests.Cli.Scaffolding;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Xunit;

namespace Fluxzy.Tests.Cli.Pack
{
    public class PackCommandTests : CommandBase
    {
        public PackCommandTests()
            : base("pack")
        {

        }

        [Fact]
        public async Task To_Fluxzy_Archive()
        {
            const string sourceDirectory = ".artefacts/tests/pink-floyd";
            var fileName = $"Drop/{nameof(To_Fluxzy_Archive)}.fxzy";

            var runResult = await InternalRun(sourceDirectory, fileName);

            Assert.Equal(0, runResult.ExitCode);
            Assert.True(File.Exists(fileName));

            // The exact archive bytes are not portable: the zip container records OS specific file
            // attributes and the deflate output depends on the runtime. Rather than pinning a hash, verify
            // the archive opens and every entry round-trips byte for byte against its source file, since the
            // packager stores the directory files verbatim.
            using var archive = new ZipArchive(File.OpenRead(fileName), ZipArchiveMode.Read);

            Assert.NotEmpty(archive.Entries);

            foreach (var entry in archive.Entries) {
                var sourcePath = Path.Combine(sourceDirectory, entry.FullName);

                Assert.True(File.Exists(sourcePath), $"Archive entry '{entry.FullName}' has no matching source file");

                using var entryStream = entry.Open();
                using var buffer = new MemoryStream();
                entryStream.CopyTo(buffer);

                Assert.Equal(File.ReadAllBytes(sourcePath), buffer.ToArray());
            }
        }

        [Fact]
        public async Task To_HttpArchive()
        {
            var fileName = $"Drop/{nameof(To_HttpArchive)}.har";

            var runResult = await InternalRun($".artefacts/tests/pink-floyd", fileName);

            Assert.Equal(0, runResult.ExitCode);
            Assert.True(File.Exists(fileName));
        }
    }
}
