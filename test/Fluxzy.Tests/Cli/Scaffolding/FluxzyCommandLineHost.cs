// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli;
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
            _standardError = new OutputWriterNotifier(true);

            _outputConsole = new OutputConsole(_standardOutput, _standardError, standardInput);
        }

        public Task<int> ExitCode { get; private set; } = null!;

        public async Task<ProxyInstance> Run(int timeoutSeconds = 5)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                timeoutSeconds = timeoutSeconds * 5;
            }

            var blockingListingTokenSource = new CancellationTokenSource();

            var blockingListenToken = blockingListingTokenSource.Token;

            var waitForPortTask = _standardOutput.WaitForValue(@"Listen.*:(\d+)$", blockingListenToken, timeoutSeconds);

            ExitCode = FluxzyStartup.Run(_commandLineArgs, _outputConsole, _cancellationToken);

            _ = ExitCode.ContinueWith(runResult => {
                if (!blockingListingTokenSource.IsCancellationRequested)
                    blockingListingTokenSource.Cancel();
                return runResult.Result;
            }, blockingListenToken);

            try
            {
                var port = int.Parse(await waitForPortTask);
                return new ProxyInstance(ExitCode, _standardOutput, _standardError, port, _cancellationTokenSource);
            }
            catch (FluxzyBadExitCodeException) {
                throw new FluxzyBadExitCodeException(
                    "Fluxzy exits with a non-zero status code.\r\n" +
                    $"Original args: {(string.Join(" ", _commandLineArgs))}\r\n" +
                    "Stderr:\r\n"
                 + _standardError.GetOutput());
            }
        }

        public static Task<ProxyInstance> CreateAndRun(string commandLine, string? standardInput = null)
        {
            return new FluxzyCommandLineHost(commandLine, standardInput: standardInput).Run();
        }
    }
}
