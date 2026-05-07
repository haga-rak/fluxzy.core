// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests._Fixtures
{
    internal enum MisbehaveMode
    {
        /// <summary>
        ///     Accept the TCP connection then close it with a zero-second linger.
        ///     Surfaces as <see cref="SocketError.ConnectionReset"/> on some stacks
        ///     and <see cref="SocketError.ConnectionAborted"/> on others — both are
        ///     abrupt-close tokens.
        /// </summary>
        AbruptClose,

        /// <summary>
        ///     Accept the TCP connection, read the TLS ClientHello, then reply with
        ///     a fatal TLS alert (handshake_failure) and close. Triggers a real TLS
        ///     handshake failure on the peer instead of a TCP-level abort.
        /// </summary>
        SendTlsHandshakeFailureAlert
    }

    internal sealed class MisbehavingTcpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly MisbehaveMode _mode;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;

        private MisbehavingTcpServer(TcpListener listener, MisbehaveMode mode)
        {
            _listener = listener;
            _mode = mode;
            _loop = AcceptLoop();
        }

        public int Port => ((IPEndPoint) _listener.LocalEndpoint).Port;

        public static MisbehavingTcpServer Start(MisbehaveMode mode)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return new MisbehavingTcpServer(listener, mode);
        }

        private async Task AcceptLoop()
        {
            try {
                while (!_cts.IsCancellationRequested) {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                    _ = HandleClient(client);
                }
            }
            catch {
                // shutting down
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            try {
                switch (_mode) {
                    case MisbehaveMode.AbruptClose:
                        client.LingerState = new LingerOption(true, 0);
                        client.Close();
                        break;

                    case MisbehaveMode.SendTlsHandshakeFailureAlert: {
                        var stream = client.GetStream();
                        var buffer = new byte[4096];

                        try {
                            // Drain the ClientHello (we don't actually parse it).
                            await stream.ReadAsync(buffer, _cts.Token).ConfigureAwait(false);
                        }
                        catch {
                            // ignore
                        }

                        // TLS Alert record: 21 (alert), 03 03 (TLS 1.2), 00 02 (length),
                        // 02 (fatal), 28 (handshake_failure = 40).
                        var alert = new byte[] { 0x15, 0x03, 0x03, 0x00, 0x02, 0x02, 40 };

                        try {
                            await stream.WriteAsync(alert, _cts.Token).ConfigureAwait(false);
                            await stream.FlushAsync(_cts.Token).ConfigureAwait(false);
                        }
                        catch {
                            // ignore
                        }

                        client.Close();
                        break;
                    }
                }
            }
            catch {
                // best effort
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _listener.Stop();

            try {
                await _loop.ConfigureAwait(false);
            }
            catch {
                // ignore
            }

            _cts.Dispose();
        }
    }
}
