// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    internal class TimeoutTaskCompletionSource<T> : IDisposable
    {
        private readonly CancellationTokenSource _tokenSource;

        public TimeoutTaskCompletionSource(int timeout, string message, CancellationToken parent)
        {
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(parent);
            _tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            _tokenSource.Token.Register(
                () => CompletionSource.TrySetException(
                    parent.IsCancellationRequested
                        ? new InvalidOperationException("CLI exited with invalid state")
                        : new TimeoutException($"Timeout was reached for this source : {message}")));
        }

        public TaskCompletionSource<T> CompletionSource { get; } = new();

        public void Dispose()
        {
            _tokenSource.Dispose();
        }
    }
}
