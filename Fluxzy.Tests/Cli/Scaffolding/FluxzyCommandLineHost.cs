// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli;
using Xunit.Abstractions;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public class FluxzyCommandLineHost
    {
        private readonly ITestOutputHelper? _outputHelper;
        private readonly string[] _commandLineArgs;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        private readonly OutputWriterNotifier _standardOutput;
        private readonly OutputWriterNotifier _standardError;
        private Task<int> _runningProxyTask;
        private readonly TextWriter _oldOutput;

        public FluxzyCommandLineHost(string commandLine, ITestOutputHelper?  outputHelper = null)
            : this(commandLine.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries))

        {
            _outputHelper = outputHelper;
        }

        public FluxzyCommandLineHost(string[] commandLineArgs)
        {
            _commandLineArgs = commandLineArgs;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _standardOutput = new OutputWriterNotifier();
            _standardError = new OutputWriterNotifier();

            _oldOutput = Console.Out; 

            Console.SetOut(_standardOutput);
            Console.SetError(_standardError);
        }

        public async Task<ProxyInstance> Run()
        {
            
            var waitForPortTask = _standardOutput.WaitForValue(@"Listen.*:(\d+)$");
            _runningProxyTask = FluxzyStartup.Run(_commandLineArgs, _cancellationToken);

            var port = int.Parse(await waitForPortTask);

            return new ProxyInstance(_runningProxyTask, _standardOutput, _standardError, port, _cancellationTokenSource);
        }

        public Task<int> ExitCode => _runningProxyTask;

        public static Task<ProxyInstance> CreateAndRun(string commandLine)
        {
            return new FluxzyCommandLineHost(commandLine).Run();
        }
    }
}