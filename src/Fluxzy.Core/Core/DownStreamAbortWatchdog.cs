// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    /// <summary>
    ///     Detects a downstream client abort (FIN/RST) while a sequential exchange is parked
    ///     on upstream I/O and cancels the connection token source so the upstream work is
    ///     released. Nothing else observes the client socket during that window, which is
    ///     also what makes the poll race-free: it only runs once the request body has been
    ///     fully consumed (data still readable then means a pipelined request, not an abort).
    /// </summary>
    internal sealed class DownStreamAbortWatchdog : IAsyncDisposable
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

        private readonly CancellationTokenSource _stopSource = new();
        private readonly Task _runningTask;

        private DownStreamAbortWatchdog(Socket socket, Exchange exchange, CancellationTokenSource callerTokenSource)
        {
            _runningTask = RunAsync(socket, exchange, callerTokenSource, _stopSource.Token);
        }

        public static DownStreamAbortWatchdog? Start(
            Socket? socket, Exchange exchange, CancellationTokenSource callerTokenSource)
        {
            if (socket == null
                || exchange.Unprocessed
                || exchange.Context.BlindMode
                || exchange.Request.Header.IsWebSocketRequest) {
                return null;
            }

            return new DownStreamAbortWatchdog(socket, exchange, callerTokenSource);
        }

        private static async Task RunAsync(
            Socket socket, Exchange exchange,
            CancellationTokenSource callerTokenSource, CancellationToken stopToken)
        {
            try {
                using var timer = new PeriodicTimer(PollInterval);

                while (await timer.WaitForNextTickAsync(stopToken).ConfigureAwait(false)) {
                    if (exchange.Metrics.RequestBodySent == default)
                        continue;

                    if (!IsSocketAborted(socket))
                        continue;

                    callerTokenSource.Cancel();

                    return;
                }
            }
            catch (OperationCanceledException) {
            }
            catch (ObjectDisposedException) {
            }
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
            _stopSource.Cancel();

            try {
                await _runningTask.ConfigureAwait(false);
            }
            catch {
                // watchdog never propagates
            }

            _stopSource.Dispose();
        }
    }
}
