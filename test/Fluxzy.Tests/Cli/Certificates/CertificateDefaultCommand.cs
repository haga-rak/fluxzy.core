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
        [InlineData("_Files/Certificates/client-cert.clear.pifix", true)]
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

        [Fact]
        public async Task List()
        {
            var runResult = await InternalRun("list");

            Assert.Equal(0, runResult.ExitCode);
        }

        [Fact]
        public async Task Export()
        {
            var runResult = await InternalRun("export", "defaultcert.pem");

            Assert.Equal(0, runResult.ExitCode);
        }

        [Theory]
        [InlineData("_Files/Certificates/client-cert.clear.pifix", 1)]
        [InlineData("_Files/Certificates/client-cert.pifix", 1)]
        [InlineData("_Files/Certificates/fluxzytest.txt", 1)]
        [InlineData(null, null)]
        public async Task Check(string? filePath, int? exitCode)
        {
            var runResult = filePath == null ?
                await InternalRun("check") : await InternalRun("check", filePath);

            if (exitCode != null)
                Assert.Equal(exitCode, runResult.ExitCode);
        }
    }
}
