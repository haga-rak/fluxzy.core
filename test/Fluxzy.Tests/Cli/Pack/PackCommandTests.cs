using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
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
            var fileName = $"Drop/{nameof(To_Fluxzy_Archive)}.fxzy";

            var runResult = await InternalRun($".artefacts/tests/pink-floyd", fileName);

            Assert.Equal(0, runResult.ExitCode);
            Assert.True(File.Exists(fileName));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Equal(Startup.DefaultArchiveHash, HashHelper.MakeWinGetHash(fileName));
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
