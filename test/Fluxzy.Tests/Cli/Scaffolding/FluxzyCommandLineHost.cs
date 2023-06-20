// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands;
using Xunit.Abstractions;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public class FluxzyCommandLineHost
    {
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly string[] _commandLineArgs;
        private readonly OutputConsole _outputConsole;
        private readonly ITestOutputHelper? _outputHelper;
        private readonly OutputWriterNotifier _standardError;
        private readonly OutputWriterNotifier _standardOutput;

        public FluxzyCommandLineHost(
            string commandLine, ITestOutputHelper? outputHelper = null, string? standardInput = null)
            : this(commandLine.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries), standardInput)

        {
            _outputHelper = outputHelper;
        }

        public FluxzyCommandLineHost(string[] commandLineArgs, string? standardInput)
        {
            _commandLineArgs = commandLineArgs;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _standardOutput = new OutputWriterNotifier();
            _standardError = new OutputWriterNotifier();

            _outputConsole = new OutputConsole(_standardOutput, _standardError, standardInput);
        }

        public Task<int> ExitCode { get; private set; }

        public async Task<ProxyInstance> Run(int timeoutSeconds = 5)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                timeoutSeconds = timeoutSeconds * 5;
            }

            var blockingListingTokenSource = new CancellationTokenSource();

            var blockingListenToken = blockingListingTokenSource.Token;

            var waitForPortTask = _standardOutput.WaitForValue(@"Listen.*:(\d+)$", blockingListenToken, timeoutSeconds);

            ExitCode = FluxzyStartup.Run(_commandLineArgs, _outputConsole, _cancellationToken);

            ExitCode.ContinueWith(runResult => {
                if (!blockingListingTokenSource.IsCancellationRequested)
                    blockingListingTokenSource.Cancel();

                return runResult.Result;
            });

            var port = int.Parse(await waitForPortTask);

            return new ProxyInstance(ExitCode, _standardOutput, _standardError, port, _cancellationTokenSource);
        }

        public static Task<ProxyInstance> CreateAndRun(string commandLine, string? standardInput = null)
        {
            return new FluxzyCommandLineHost(commandLine, standardInput: standardInput).Run();
        }
    }
}
