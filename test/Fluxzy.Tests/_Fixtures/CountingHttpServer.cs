// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests._Fixtures
{
    /// <summary>
    ///     A loopback HTTP/1.1 server that answers every request with an empty
    ///     <c>200 OK</c> on a keep-alive connection and counts how many TCP connections
    ///     it accepted. Used to assert how many upstream sockets a pool actually opens.
    /// </summary>
    internal sealed class CountingHttpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _acceptLoop;
        private readonly bool _gateResponseBodies;
        private readonly TaskCompletionSource _responseBodyGate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _acceptedConnections;

        private CountingHttpServer(TcpListener listener, bool gateResponseBodies)
        {
            _listener = listener;
            _gateResponseBodies = gateResponseBodies;

            if (!gateResponseBodies)
                _responseBodyGate.TrySetResult();

            _acceptLoop = AcceptLoop();
        }

        public int Port => ((IPEndPoint) _listener.LocalEndpoint).Port;

        public int AcceptedConnections => Volatile.Read(ref _acceptedConnections);

        public static CountingHttpServer Start(bool gateResponseBodies = false)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            return new CountingHttpServer(listener, gateResponseBodies);
        }

        public void ReleaseResponseBodies()
        {
            _responseBodyGate.TrySetResult();
        }

        private async Task AcceptLoop()
        {
            try {
                while (!_cts.IsCancellationRequested) {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                    Interlocked.Increment(ref _acceptedConnections);
                    _ = HandleConnection(client);
                }
            }
            catch {
                // shutting down
            }
        }

        private async Task HandleConnection(TcpClient client)
        {
            try {
                using var _ = client;
                var stream = client.GetStream();
                var buffer = new byte[8192];
                var accumulated = new StringBuilder();

                var responseHeader = Encoding.ASCII.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Length: {(_gateResponseBodies ? 1 : 0)}\r\n" +
                    "Connection: keep-alive\r\n\r\n");

                while (!_cts.IsCancellationRequested) {
                    var read = await stream.ReadAsync(buffer, _cts.Token).ConfigureAwait(false);

                    if (read == 0)
                        return;

                    accumulated.Append(Encoding.ASCII.GetString(buffer, 0, read));

                    // Answer once per complete header block (GET only, no request body).
                    while (true) {
                        var text = accumulated.ToString();
                        var end = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);

                        if (end < 0)
                            break;

                        accumulated.Remove(0, end + 4);

                        await stream.WriteAsync(responseHeader, _cts.Token).ConfigureAwait(false);
                        await stream.FlushAsync(_cts.Token).ConfigureAwait(false);

                        if (_gateResponseBodies) {
                            await _responseBodyGate.Task.WaitAsync(_cts.Token).ConfigureAwait(false);
                            await stream.WriteAsync(new byte[] { (byte) 'x' }, _cts.Token).ConfigureAwait(false);
                            await stream.FlushAsync(_cts.Token).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch {
                // connection torn down
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _listener.Stop();

            try {
                await _acceptLoop.ConfigureAwait(false);
            }
            catch {
                // ignore
            }

            _cts.Dispose();
        }
    }
}
