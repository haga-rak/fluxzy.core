// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    /// <summary>
    ///     Detects a downstream client abort (FIN/RST) while a sequential exchange is parked
    ///     on upstream work (pool acquisition, connect, TLS handshake or response wait) and
    ///     cancels the connection token source so that work is released. One instance lives
    ///     per downstream connection; exchanges are armed and disarmed via
    ///     <see cref="Watch"/>/<see cref="Unwatch"/>. The poll only runs while nothing else
    ///     reads the client socket: either before anything has been sent upstream, or once
    ///     the request body has been fully forwarded (data still readable then means a
    ///     pipelined request, not an abort). The gate guarantees no cancel can fire after
    ///     Unwatch returns.
    /// </summary>
    internal sealed class DownStreamAbortWatchdog : IAsyncDisposable
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

        private readonly Socket _socket;
        private readonly CancellationTokenSource _callerTokenSource;
        private readonly PeriodicTimer _timer = new(PollInterval);
        private readonly Task _runningTask;
        private readonly object _gate = new();

        private Exchange? _watched;

        public DownStreamAbortWatchdog(Socket socket, CancellationTokenSource callerTokenSource)
        {
            _socket = socket;
            _callerTokenSource = callerTokenSource;
            _runningTask = RunAsync();
        }

        public static bool IsEligible(Exchange exchange)
        {
            return !exchange.Unprocessed
                   && !exchange.Context.BlindMode
                   && !exchange.Request.Header.IsWebSocketRequest;
        }

        public void Watch(Exchange exchange)
        {
            lock (_gate) {
                _watched = exchange;
            }
        }

        public void Unwatch()
        {
            lock (_gate) {
                _watched = null;
            }
        }

        private async Task RunAsync()
        {
            try {
                while (await _timer.WaitForNextTickAsync().ConfigureAwait(false)) {
                    lock (_gate) {
                        var exchange = _watched;

                        if (exchange == null || !IsPollSafe(exchange))
                            continue;

                        if (!IsSocketAborted(_socket))
                            continue;

                        _watched = null;
                        _callerTokenSource.Cancel();

                        return;
                    }
                }
            }
            catch (ObjectDisposedException) {
            }
        }

        private static bool IsPollSafe(Exchange exchange)
        {
            if (exchange.Metrics.RequestBodySent != default)
                return true;

            // Before the upstream send starts, only a request body substitution may
            // read the client socket.
            return exchange.Metrics.RequestHeaderSending == default
                   && !exchange.Context.HasRequestBodySubstitution;
        }

        private static bool IsSocketAborted(Socket socket)
        {
            try {
                return socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0;
            }
            catch {
                return true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _timer.Dispose();

            try {
                await _runningTask.ConfigureAwait(false);
            }
            catch {
                // watchdog never propagates
            }
        }
    }
}
