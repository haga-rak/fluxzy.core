// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests.Cli
{
    public class ProxyInstance : IAsyncDisposable
    {
        private readonly Task _proxyTask;
        private readonly OutputWriterNotifier _standardOutput;
        private readonly OutputWriterNotifier _standardError;
        private readonly CancellationTokenSource _tokenSource;

        public ProxyInstance(Task proxyTask,
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

        public Task<string> WaitForRegexOnStandardOutput(string regex, int timeoutSeconds)
        {
            return _standardOutput.WaitForValue(regex, timeoutSeconds);
        }

        public Task<string> WaitForRegexOnStandardError(string regex, int timeoutSeconds)
        {
            return _standardError.WaitForValue(regex, timeoutSeconds);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Cancel();

                await _proxyTask;

                _tokenSource.Dispose();
            }
        }
    }
}