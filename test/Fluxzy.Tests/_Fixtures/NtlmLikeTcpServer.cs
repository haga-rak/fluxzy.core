// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests._Fixtures
{
    /// <summary>
    ///     A minimal HTTP/1.1 server emulating connection-oriented authentication
    ///     (NTLM / Negotiate-style, as used by SSPI on Windows). The crypto is faked,
    ///     the transport semantics are the real thing:
    ///     - the challenge issued for a TYPE1 message is scoped to the TCP connection,
    ///     - the TYPE3 reply is only accepted on the connection that got the challenge,
    ///       and any other request in between drops the pending security context,
    ///     - once authenticated, the connection itself is authenticated: subsequent
    ///       requests on it succeed without any Authorization header.
    ///
    ///     Protocol accepted on the Authorization header:
    ///     - "NTLM TYPE1"                    → 401 + "WWW-Authenticate: NTLM {challenge}"
    ///     - "NTLM TYPE3 {challenge} {user}" → 200 if {challenge} is the one pending on
    ///       this connection, else 401 restart
    ///     - anything else / absent          → 401 + "WWW-Authenticate: NTLM"
    /// </summary>
    internal sealed class NtlmLikeTcpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _acceptLoop;
        private readonly bool _dropOnceAfterChallenge;
        private int _connectionCounter;
        private int _challengeCounter;
        private int _dropArmed;

        public ConcurrentQueue<string> RequestLog { get; } = new();

        private NtlmLikeTcpServer(TcpListener listener, bool dropOnceAfterChallenge)
        {
            _listener = listener;
            _dropOnceAfterChallenge = dropOnceAfterChallenge;
            _dropArmed = dropOnceAfterChallenge ? 1 : 0;
            _acceptLoop = AcceptLoop();
        }

        public int Port => ((IPEndPoint) _listener.LocalEndpoint).Port;

        public int TotalConnections => Volatile.Read(ref _connectionCounter);

        /// <param name="dropOnceAfterChallenge">
        ///     When true, the server closes the connection right after issuing its first
        ///     challenge, emulating an upstream connection dying mid-handshake.
        /// </param>
        public static NtlmLikeTcpServer Start(bool dropOnceAfterChallenge = false)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            return new NtlmLikeTcpServer(listener, dropOnceAfterChallenge);
        }

        private async Task AcceptLoop()
        {
            try {
                while (!_cts.IsCancellationRequested) {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                    var connectionId = Interlocked.Increment(ref _connectionCounter);
                    _ = HandleConnection(client, connectionId);
                }
            }
            catch {
                // shutting down
            }
        }

        private async Task HandleConnection(TcpClient client, int connectionId)
        {
            string? pendingChallenge = null;
            string? authenticatedUser = null;

            try {
                using var _ = client;
                var stream = client.GetStream();

                while (!_cts.IsCancellationRequested) {
                    var headers = await ReadHeaderBlock(stream).ConfigureAwait(false);

                    if (headers == null)
                        return; // peer closed

                    var authorization = GetHeader(headers, "Authorization");

                    int status;
                    string? wwwAuthenticate = null;
                    string body;

                    if (authenticatedUser != null) {
                        // NTLM semantics: the connection carries the identity from now on
                        status = 200;
                        body = $"user={authenticatedUser};conn={connectionId}";
                    }
                    else if (authorization == "NTLM TYPE1") {
                        pendingChallenge = $"CH-{connectionId}-{Interlocked.Increment(ref _challengeCounter)}";
                        status = 401;
                        wwwAuthenticate = $"NTLM {pendingChallenge}";
                        body = "challenge issued";
                    }
                    else if (authorization != null && authorization.StartsWith("NTLM TYPE3 ", StringComparison.Ordinal)) {
                        var parts = authorization.Substring("NTLM TYPE3 ".Length)
                                                 .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                        var providedChallenge = parts.Length > 0 ? parts[0] : "";
                        var user = parts.Length > 1 ? parts[1] : "anonymous";

                        if (pendingChallenge != null && providedChallenge == pendingChallenge) {
                            authenticatedUser = user;
                            pendingChallenge = null;
                            status = 200;
                            body = $"user={user};conn={connectionId}";
                        }
                        else {
                            // TYPE3 on a connection that holds no matching security
                            // context: restart the handshake, as SSPI does
                            pendingChallenge = null;
                            status = 401;
                            wwwAuthenticate = "NTLM";
                            body = "handshake restarted";
                        }
                    }
                    else {
                        // an unauthenticated request drops any half-open handshake
                        pendingChallenge = null;
                        status = 401;
                        wwwAuthenticate = "NTLM";
                        body = "authentication required";
                    }

                    RequestLog.Enqueue($"conn={connectionId};auth={authorization ?? "(none)"};status={status}");

                    await WriteResponse(stream, status, wwwAuthenticate, body, connectionId).ConfigureAwait(false);

                    // Emulate an upstream connection dying right after a challenge (once).
                    if (_dropOnceAfterChallenge && pendingChallenge != null
                        && Interlocked.Exchange(ref _dropArmed, 0) == 1)
                        return;
                }
            }
            catch {
                // connection torn down
            }
        }

        private static async Task<List<string>?> ReadHeaderBlock(NetworkStream stream)
        {
            var buffer = new byte[16 * 1024];
            var received = 0;

            while (true) {
                var read = await stream.ReadAsync(buffer, received, buffer.Length - received).ConfigureAwait(false);

                if (read == 0)
                    return null;

                received += read;

                var text = Encoding.ASCII.GetString(buffer, 0, received);
                var endOfHeaders = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);

                if (endOfHeaders >= 0) {
                    // GET only: nothing to drain after the header block
                    var lines = text.Substring(0, endOfHeaders).Split("\r\n");

                    return new List<string>(lines);
                }

                if (received == buffer.Length)
                    throw new InvalidOperationException("Header block too large");
            }
        }

        private static string? GetHeader(List<string> headerLines, string name)
        {
            foreach (var line in headerLines) {
                var separator = line.IndexOf(':');

                if (separator <= 0)
                    continue;

                if (string.Equals(line.Substring(0, separator).Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(separator + 1).Trim();
            }

            return null;
        }

        private static async Task WriteResponse(
            NetworkStream stream, int status, string? wwwAuthenticate, string body, int connectionId)
        {
            var reason = status == 200 ? "OK" : "Unauthorized";
            var payload = Encoding.UTF8.GetBytes(body);

            var builder = new StringBuilder();
            builder.Append($"HTTP/1.1 {status} {reason}\r\n");
            builder.Append($"Content-Length: {payload.Length}\r\n");
            builder.Append("Content-Type: text/plain\r\n");
            builder.Append("Connection: keep-alive\r\n");
            builder.Append($"X-Conn-Id: {connectionId}\r\n");

            if (wwwAuthenticate != null)
                builder.Append($"WWW-Authenticate: {wwwAuthenticate}\r\n");

            builder.Append("\r\n");

            var header = Encoding.ASCII.GetBytes(builder.ToString());

            await stream.WriteAsync(header).ConfigureAwait(false);
            await stream.WriteAsync(payload).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
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
