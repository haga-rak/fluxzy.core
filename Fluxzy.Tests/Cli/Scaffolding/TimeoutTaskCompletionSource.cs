// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    internal class TimeoutTaskCompletionSource<T> : IDisposable
    {
        private readonly CancellationTokenSource _tokenSource;

        public TimeoutTaskCompletionSource(int timeout)
        {
            _tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            _tokenSource.Token.Register(
                () => CompletionSource.TrySetException(new TimeoutException($"Timeout was reached for this source")));

        }

        public TaskCompletionSource<T> CompletionSource { get; } = new();

        public void Dispose()
        {
            _tokenSource.Dispose();
        }
    }
}