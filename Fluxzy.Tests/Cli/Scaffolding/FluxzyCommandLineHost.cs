// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public class FluxzyCommandLineHost
    {
        private readonly string[] _commandLineArgs;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        private readonly OutputWriterNotifier _standardOutput;
        private readonly OutputWriterNotifier _standardError;

        public FluxzyCommandLineHost(string commandLine)
            : this(commandLine.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries))

        {
        }

        public FluxzyCommandLineHost(string[] commandLineArgs)
        {
            _commandLineArgs = commandLineArgs;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _standardOutput = new OutputWriterNotifier();
            _standardError = new OutputWriterNotifier();

            Console.SetOut(_standardOutput);
            Console.SetError(_standardError);
        }

        public async Task<ProxyInstance> Run()
        {
            var waitForPortTask = _standardOutput.WaitForValue(@"Listen.*:(\d+)$");
            var runningProxyTask = FluxzyStartup.Run(_commandLineArgs, _cancellationToken);

            var port = int.Parse(await waitForPortTask);

            return new ProxyInstance(runningProxyTask, _standardOutput, _standardError, port, _cancellationTokenSource);
        }
    }
}