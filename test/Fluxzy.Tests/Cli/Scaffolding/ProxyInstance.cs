// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public class ProxyInstance : IAsyncDisposable
    {
        private readonly Task _proxyTask;
        private readonly OutputWriterNotifier _standardError;
        private readonly OutputWriterNotifier _standardOutput;
        private readonly CancellationTokenSource _tokenSource;

        public ProxyInstance(
            Task proxyTask,
            OutputWriterNotifier standardOutput,
            OutputWriterNotifier standardError,
            int listenPort, CancellationTokenSource tokenSource)
        {
            ListenPort = listenPort;
            _proxyTask = proxyTask;
            _standardOutput = standardOutput;
            _standardError = standardError;
            _tokenSource = tokenSource;
        }

        public int ListenPort { get; }

        public async ValueTask DisposeAsync()
        {
            if (!_tokenSource.IsCancellationRequested) {
                _tokenSource.Cancel();

                try {

                    await _proxyTask;
                }
                catch (TaskCanceledException) {
                    // Ignore cancelling
                }

                _tokenSource.Dispose();
            }
        }

        public Task<string> WaitForRegexOnStandardOutput(string regex, int timeoutSeconds)
        {
            return _standardOutput.WaitForValue(regex, CancellationToken.None, timeoutSeconds);
        }

        public Task<string> WaitForRegexOnStandardError(string regex, int timeoutSeconds)
        {
            return _standardError.WaitForValue(regex, CancellationToken.None, timeoutSeconds);
        }
    }
}
