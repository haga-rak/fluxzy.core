// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli.Certificates
{
    public class CertificateDefaultCommand : CommandBase
    {
        public CertificateDefaultCommand()
            : base("certificate", true)
        {

        }

        [Fact]
        public async Task Get()
        {
            var runResult = await InternalRun("default");

            Assert.Equal(0, runResult.ExitCode);
            Assert.Contains("[Subject]", runResult.StdOut);
            Assert.Contains("[Private Key]", runResult.StdOut);
        }

        [Theory]
        [InlineData("_Files/Certificates/client-cert.clear.pifix", false)]
        [InlineData("_Files/Certificates/client-cert.pifix", true)]
        [InlineData("_Files/Certificates/fluxzytest.txt", false)]
        [InlineData("_Files/Certificates/missing_file.txt", false)]
        public async Task Set(string certPath, bool succeed)
        {
            var directory = "set_test";

            if (System.IO.Directory.Exists(directory)) {
                System.IO.Directory.Delete(directory, true);
            }

            var environmentProvider = new DictionaryEnvironmentProvider(new () {
                ["appdata"] = directory
            });

            var runResult = await InternalRun(environmentProvider, "default", certPath);

            if (succeed) {
                Assert.Equal(0, runResult.ExitCode);
            }
            else {
                Assert.NotEqual(0, runResult.ExitCode);
            }
        }
    }
}
