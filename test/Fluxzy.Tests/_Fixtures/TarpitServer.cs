// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests._Fixtures
{
    /// <summary>
    ///     Upstream that accepts the connection (and the TLS handshake), reads the request
    ///     and never answers. With <c>answerFirstRequest</c> the first request of each
    ///     connection gets a keep-alive chunked 200 and only the next one hangs.
    /// </summary>
    internal sealed class TarpitServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly bool _useTls;
        private readonly TimeSpan? _respondAfter;
        private readonly bool _answerFirstRequest;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _acceptLoop;

        private readonly TaskCompletionSource<bool> _tornDown =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly X509Certificate2? _certificate;

        private int _requestCount;

        private TarpitServer(bool useTls, TimeSpan? respondAfter, bool answerFirstRequest)
        {
            _useTls = useTls;
            _respondAfter = respondAfter;
            _answerFirstRequest = answerFirstRequest;

            if (useTls) {
                _certificate = new X509Certificate2(
                    "_Files/Certificates/client-cert.pifix",
                    CertificateContext.DefaultPassword);
            }

            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
            _acceptLoop = AcceptLoopAsync();
        }

        public int Port { get; }

        public static TarpitServer Start(bool useTls, TimeSpan? respondAfter = null, bool answerFirstRequest = false)
        {
            return new TarpitServer(useTls, respondAfter, answerFirstRequest);
        }

        public async Task<bool> WaitForRequestCount(int count, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;

            while (Volatile.Read(ref _requestCount) < count) {
                if (DateTime.UtcNow > deadline)
                    return false;

                await Task.Delay(20);
            }

            return true;
        }

        public async Task<bool> WaitForTeardown(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_tornDown.Task, Task.Delay(timeout, _cts.Token));

            return completed == _tornDown.Task;
        }

        private async Task AcceptLoopAsync()
        {
            try {
                while (!_cts.IsCancellationRequested) {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = HandleClientAsync(client);
                }
            }
            catch {
                // listener stopped
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try {
                using var _ = client;

                Stream stream = client.GetStream();

                if (_useTls) {
                    var sslStream = new SslStream(stream, false);

                    await sslStream.AuthenticateAsServerAsync(_certificate!,
                        clientCertificateRequired: false,
                        enabledSslProtocols: SslProtocols.Tls12,
                        checkCertificateRevocation: false);

                    stream = sslStream;
                }

                var buffer = new byte[16 * 1024];
                var received = new StringBuilder();
                var requestCount = 0;

                while (true) {
                    var read = await stream.ReadAsync(buffer, _cts.Token);

                    if (read == 0)
                        return;

                    received.Append(Encoding.ASCII.GetString(buffer, 0, read));

                    var endOfHeaders = received.ToString().IndexOf("\r\n\r\n", StringComparison.Ordinal);

                    if (endOfHeaders < 0)
                        continue;

                    received.Remove(0, endOfHeaders + 4);
                    requestCount++;
                    Interlocked.Increment(ref _requestCount);

                    if (_answerFirstRequest && requestCount == 1) {
                        var keepAliveResponse =
                            "HTTP/1.1 200 OK\r\n" +
                            "Transfer-Encoding: chunked\r\n" +
                            "Connection: keep-alive\r\n" +
                            "\r\n" +
                            "2\r\nok\r\n0\r\n\r\n";

                        await stream.WriteAsync(Encoding.ASCII.GetBytes(keepAliveResponse), _cts.Token);

                        continue;
                    }

                    if (_respondAfter != null) {
                        await Task.Delay(_respondAfter.Value, _cts.Token);

                        var response =
                            "HTTP/1.1 200 OK\r\n" +
                            "Content-Length: 2\r\n" +
                            "Connection: close\r\n" +
                            "\r\n" +
                            "ok";

                        await stream.WriteAsync(Encoding.ASCII.GetBytes(response), _cts.Token);

                        return;
                    }

                    // Tarpit: never answer, keep draining until the peer goes away
                }
            }
            catch {
                // FIN/RST/abort observed
            }
            finally {
                _tornDown.TrySetResult(true);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _listener.Stop();

            try {
                await _acceptLoop;
            }
            catch {
            }

            _certificate?.Dispose();
            _cts.Dispose();
        }
    }
}
