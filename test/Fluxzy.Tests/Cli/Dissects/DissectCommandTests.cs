// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading;
using Fluxzy.Cli.Commands;
using System.Threading.Tasks;
using Fluxzy.Tests.Cli.Scaffolding;

namespace Fluxzy.Tests.Cli.Dissects
{
    public class DissectCommandTests
    {
        public async Task TestRead()
        {
            var standardOutput = new OutputWriterNotifier();
            var standardError = new OutputWriterNotifier();

            var outputConsole = new OutputConsole(standardOutput, standardError, "");

            await FluxzyStartup.Run(new[] { "dissect", "read", "test" },
                outputConsole, CancellationToken.None);


        }
    }
}
