// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands;
using Fluxzy.Tests.Cli.Scaffolding;

namespace Fluxzy.Tests.Cli.Dissects
{
    public abstract class DissectCommandBase
    {
        private readonly OutputConsole _outputConsole;

        protected DissectCommandBase()
        {
            var standardOutput = new OutputWriterNotifier();
            var standardError = new OutputWriterNotifier();
            _outputConsole = new OutputConsole(standardOutput, standardError, "");
        }

        protected async Task<RunResult> InternalRun(string fileName, params string[] options)
        {
            var args=  new[] { "dissect", fileName }.Concat(options).ToArray();
            var exitCode = await FluxzyStartup.Run(args, _outputConsole, CancellationToken.None);

            return new RunResult(exitCode, _outputConsole.BinaryStdout, _outputConsole.BinaryStderr);
        }
    }
}
