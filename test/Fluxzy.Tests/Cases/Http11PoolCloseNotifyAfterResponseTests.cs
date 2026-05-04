// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    /// <summary>
    ///     Reproducer for the production trace where Fluxzy (Http11ConnectionPool +
    ///     BouncyCastle TLS) tries to send a third HTTP/1.1 request on a connection
    ///     after the server has already emitted a TLS close_notify + TCP FIN
    ///     immediately following response #2 — without ever advertising
    ///     <c>Connection: close</c> at the HTTP layer.
    ///
    ///     Wireshark timeline of the original incident:
    ///         req#1 -> resp#1 (200)                        keep-alive
    ///         req#2 -> resp#2 (200)                        keep-alive
    ///                  server: TLS close_notify (warn)
    ///                  server: TCP FIN
    ///         req#3 -> [written on dead conn]              client TLS close_notify
    ///                  server: RST x N
    ///
    ///     The pool happily recycled the connection because nothing at the HTTP
    ///     layer told it to close. Detection has to come from the TLS layer (read
    ///     returns 0 / throws fast) — and Fluxzy's
    ///     <c>Http11HeaderBlockReader</c> only catches that on the *response*
    ///     read of the next request, throwing <c>ConnectionCloseException
    ///     ("Relaunch")</c> which <c>ProxyOrchestrator</c> turns into a retry on
    ///     a fresh connection.
    ///
    ///     This test stands up a raw TLSv1.2 listener that mimics the trace,
    ///     pipes traffic through a real Fluxzy proxy (parametric on the
    ///     BouncyCastle vs OS SSL engine) and asserts that all three requests
    ///     return 200. If the recycle-on-close-notify path is broken on a given
    ///     engine, the third request fails (IOException / HttpRequestException /
    ///     ConnectionCloseException leaking out of the pool).
    /// </summary>
    public class Http11PoolCloseNotifyAfterResponseTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Third_Request_Succeeds_When_Server_Sends_CloseNotify_After_Response_Two(bool useBouncyCastle)
        {
            const int requestsPerConnection = 2;
            const int totalRequests = 3;

            await using var server = await CloseNotifyAfterNthResponseServer.StartAsync(requestsPerConnection);

            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(30);

            var url = $"https://local.fluxzy.io:{server.Port}/";

            for (var i = 0; i < totalRequests; i++) {
                // Match the ~127 ms spacing seen in the production trace between
                // response #2 and request #3 — that's what gives the server's
                // close_notify + FIN time to land on the client side and become
                // observable on the next read.
                if (i > 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(150));

                using var response = await client.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("ok", body);
            }

            // The first TCP connection serves requests 1 and 2 then dies; request
            // 3 must have triggered a fresh connection — assert that the server
            // actually saw a second TCP accept.
            Assert.True(server.AcceptedConnectionCount >= 2,
                $"Expected at least 2 upstream TCP connections (the first dies after " +
                $"response #2 + close_notify, request #3 must reconnect), but saw " +
                $"{server.AcceptedConnectionCount}. Engine = " +
                $"{(useBouncyCastle ? "BouncyCastle" : "OSDefault")}.");
        }

        /// <summary>
        ///     A raw TLSv1.2 HTTP/1.1 listener that, on every accepted TCP
        ///     connection, replies to <see cref="_requestsPerConnection"/>
        ///     requests with a vanilla 200 (Content-Length, no
        ///     <c>Connection: close</c>) and then immediately:
        ///       1. sends a TLS close_notify alert (via
        ///          <see cref="SslStream.ShutdownAsync"/>);
        ///       2. closes the underlying TCP socket (FIN).
        ///     This is the exact server-side behaviour captured in the
        ///     production Wireshark trace that this test reproduces.
        /// </summary>
        private sealed class CloseNotifyAfterNthResponseServer : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly X509Certificate2 _certificate;
            private readonly int _requestsPerConnection;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _acceptLoop;

            private int _acceptedCount;

            public int Port { get; }

            public int AcceptedConnectionCount => Volatile.Read(ref _acceptedCount);

            private CloseNotifyAfterNthResponseServer(
                TcpListener listener, X509Certificate2 certificate, int requestsPerConnection)
            {
                _listener = listener;
                _certificate = certificate;
                _requestsPerConnection = requestsPerConnection;
                Port = ((IPEndPoint)listener.LocalEndpoint).Port;
                _acceptLoop = Task.Run(AcceptLoopAsync);
            }

            public static Task<CloseNotifyAfterNthResponseServer> StartAsync(int requestsPerConnection)
            {
                var certificate = new X509Certificate2(
                    "_Files/Certificates/client-cert.pifix",
                    CertificateContext.DefaultPassword);

                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();

                return Task.FromResult(new CloseNotifyAfterNthResponseServer(
                    listener, certificate, requestsPerConnection));
            }

            private async Task AcceptLoopAsync()
            {
                var token = _cts.Token;

                try {
                    while (!token.IsCancellationRequested) {
                        var tcpClient = await _listener.AcceptTcpClientAsync(token);
                        Interlocked.Increment(ref _acceptedCount);
                        _ = Task.Run(() => HandleClientAsync(tcpClient, token), token);
                    }
                }
                catch (OperationCanceledException) {
                    // Normal shutdown.
                }
                catch (ObjectDisposedException) {
                    // Listener stopped while accept was pending.
                }
            }

            private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken token)
            {
                using var _ = tcpClient;
                tcpClient.NoDelay = true;

                var networkStream = tcpClient.GetStream();
                await using var sslStream = new SslStream(networkStream, leaveInnerStreamOpen: false);

                try {
                    await sslStream.AuthenticateAsServerAsync(
                        _certificate,
                        clientCertificateRequired: false,
                        enabledSslProtocols: SslProtocols.Tls12,
                        checkCertificateRevocation: false);
                }
                catch {
                    return;
                }

                for (var i = 0; i < _requestsPerConnection; i++) {
                    if (token.IsCancellationRequested)
                        return;

                    string request;

                    try {
                        request = await ReadHttpRequestAsync(sslStream, token);
                    }
                    catch {
                        return;
                    }

                    if (string.IsNullOrEmpty(request))
                        return;

                    const string body = "ok";
                    var headers =
                        "HTTP/1.1 200 OK\r\n" +
                        "Server: fluxzy-test\r\n" +
                        $"Content-Length: {body.Length}\r\n" +
                        "\r\n" +
                        body;

                    var bytes = Encoding.ASCII.GetBytes(headers);

                    try {
                        await sslStream.WriteAsync(bytes, token);
                        await sslStream.FlushAsync(token);
                    }
                    catch {
                        return;
                    }
                }

                // Mirror the trace: emit TLS close_notify, then FIN. ShutdownAsync
                // sends close_notify; the using/Dispose chain triggers the FIN.
                try {
                    await sslStream.ShutdownAsync();
                }
                catch {
                    // Ignore — peer may already be gone.
                }
            }

            private static async Task<string> ReadHttpRequestAsync(Stream stream, CancellationToken token)
            {
                var buffer = new byte[1];
                var sb = new StringBuilder(256);
                var match = 0;

                while (match < 4) {
                    var read = await stream.ReadAsync(buffer.AsMemory(0, 1), token);
                    if (read == 0)
                        return sb.ToString();

                    sb.Append((char)buffer[0]);

                    match = buffer[0] switch {
                        (byte)'\r' when match % 2 == 0 => match + 1,
                        (byte)'\n' when match % 2 == 1 => match + 1,
                        _ => 0
                    };
                }

                return sb.ToString();
            }

            public async ValueTask DisposeAsync()
            {
                _cts.Cancel();

                try {
                    _listener.Stop();
                }
                catch {
                    // Ignore.
                }

                try {
                    await _acceptLoop;
                }
                catch {
                    // Ignore.
                }

                _cts.Dispose();
                _certificate.Dispose();
            }
        }
    }
}
