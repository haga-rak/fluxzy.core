// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli.Pack
{
    public class DissectPcapTests : CommandBase
    {
        public DissectPcapTests()
            : base("dis")
        {

        }

        [Fact]
        public async Task Export_From_Directory()
        {
            var fileName = $"Drop/{nameof(Export_From_Directory)}.pcapng";

            var runResult = await InternalRun("pcap", ".artefacts/tests/pink-floyd", "-o", fileName);

            Assert.Equal(0, runResult.ExitCode);
            Assert.True(File.Exists(fileName));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Equal(Startup.DefaultMergeArchivePcapHash, HashHelper.MakeWinGetHash(fileName));
        }

        [Fact]
        public async Task Export_From_Archive()
        {
            var fileName = $"Drop/{nameof(Export_From_Archive)}.pcapng";

            var runResult = await InternalRun("pcap", "_Files/Archives/pink-floyd.fxzy", "-o", fileName);

            Assert.Equal(0, runResult.ExitCode);
            Assert.True(File.Exists(fileName));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Equal(Startup.DefaultMergeArchivePcapHash, HashHelper.MakeWinGetHash(fileName));
        }

        [Fact]
        public async Task Export_Invalid()
        {
            var fileName = $"Drop/{nameof(Export_Invalid)}.pcapng";

            var runResult = await InternalRun("pcap", "_non_existing_file", "-o", fileName);

            Assert.Equal(1, runResult.ExitCode);
            Assert.False(File.Exists(fileName));}
    }
}
