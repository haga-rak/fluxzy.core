// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Tests._Fixtures
{
    /// <summary>
    ///     Minimal HTTP/2 server mock that records every incoming frame and fails the
    ///     calling test if the client (Fluxzy, in these tests) ever violates the RFC
    ///     9113 stream state machine by sending more frames on a stream that already
    ///     received END_STREAM from its peer.
    ///
    ///     It is NOT a full H2 server: it only handshakes, answers requests on a
    ///     single stream with an empty 200 response, and quietly drains everything
    ///     else until the test is torn down.
    /// </summary>
    internal sealed class StrictH2Recorder : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly X509Certificate2 _serverCert;
        private readonly CancellationTokenSource _cts = new();
        private readonly TaskCompletionSource<object?> _done =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _acceptTask;
        private Exception? _recordedError;

        private StrictH2Recorder(TcpListener listener, X509Certificate2 serverCert)
        {
            _listener = listener;
            _serverCert = serverCert;
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public static Task<StrictH2Recorder> StartAsync(X509Certificate2 serverCert)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var recorder = new StrictH2Recorder(listener, serverCert);
            recorder._acceptTask = recorder.RunAsync();
            return Task.FromResult(recorder);
        }

        private async Task RunAsync()
        {
            try {
                using var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                await using var sslStream = new SslStream(client.GetStream(), leaveInnerStreamOpen: false);

                await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions {
                    ServerCertificate = _serverCert,
                    ClientCertificateRequired = false,
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                                          | System.Security.Authentication.SslProtocols.Tls13,
                    ApplicationProtocols = new List<SslApplicationProtocol> {
                        SslApplicationProtocol.Http2
                    },
                }, _cts.Token);

                await HandleConnectionAsync(sslStream, _cts.Token);
            }
            catch (OperationCanceledException) {
                // expected on dispose
            }
            catch (Exception ex) {
                _recordedError ??= ex;
            }
            finally {
                _done.TrySetResult(null);
            }
        }

        private async Task HandleConnectionAsync(SslStream stream, CancellationToken token)
        {
            // Read the H2 preface.
            var prefaceBuffer = new byte[H2Constants.Preface.Length];
            await ReadExactAsync(stream, prefaceBuffer, token);

            // Send empty server SETTINGS.
            var emptySettings = new byte[9];
            emptySettings[3] = (byte)H2FrameType.Settings;
            await stream.WriteAsync(emptySettings.AsMemory(), token);
            await stream.FlushAsync(token);

            var streamsWithEndStream = new HashSet<int>();
            var streamsAnswered = new HashSet<int>();

            while (!token.IsCancellationRequested) {
                var header = new byte[9];
                var read = await ReadAtMostAsync(stream, header, token);
                if (read == 0)
                    return; // peer closed

                if (read < 9)
                    throw new InvalidDataException("Incomplete frame header from Fluxzy upstream");

                var bodyLength = (header[0] << 16) | (header[1] << 8) | header[2];
                var frameType = (H2FrameType)header[3];
                var flags = (HeaderFlags)header[4];
                var streamId = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(5, 4)) & 0x7FFFFFFF;
                var body = bodyLength == 0 ? Array.Empty<byte>() : new byte[bodyLength];
                if (bodyLength > 0)
                    await ReadExactAsync(stream, body, token);

                // The actual regression check: once a stream has had END_STREAM set,
                // no more HEADERS/DATA/CONTINUATION frames are allowed on it.
                if (streamId != 0 && streamsWithEndStream.Contains(streamId)
                    && (frameType == H2FrameType.Data
                        || frameType == H2FrameType.Headers
                        || frameType == H2FrameType.Continuation)) {
                    _recordedError = new InvalidOperationException(
                        $"Fluxzy sent a {frameType} frame on stream {streamId} after END_STREAM. " +
                        "This is the regression the fix is meant to prevent.");
                    return;
                }

                if (flags.HasFlag(HeaderFlags.EndStream))
                    streamsWithEndStream.Add(streamId);

                // On the first HEADERS[END_STREAM] from the client, answer with an
                // empty 200 so Fluxzy's downstream client sees a response and the
                // test's foreground side can move forward.
                if (frameType == H2FrameType.Headers
                    && streamId != 0
                    && flags.HasFlag(HeaderFlags.EndStream)
                    && streamsAnswered.Add(streamId)) {
                    await WriteStatus200EndStreamAsync(stream, streamId, token);
                }
            }
        }

        /// <summary>
        ///     Sends a HEADERS frame containing only ":status: 200", with END_HEADERS
        ///     and END_STREAM set. The payload is hand-coded: it uses HPACK index 8
        ///     (":status: 200" from the static table) which is a single 0x88 byte.
        /// </summary>
        private static async Task WriteStatus200EndStreamAsync(
            Stream stream, int streamId, CancellationToken token)
        {
            var frame = new byte[9 + 1];
            // length = 1
            frame[0] = 0x00;
            frame[1] = 0x00;
            frame[2] = 0x01;
            frame[3] = (byte)H2FrameType.Headers;
            frame[4] = (byte)(HeaderFlags.EndHeaders | HeaderFlags.EndStream);
            BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(5, 4), streamId);
            frame[9] = 0x88; // :status 200, indexed header, static table index 8

            await stream.WriteAsync(frame.AsMemory(), token);
            await stream.FlushAsync(token);
        }

        public async Task StopAndVerifyAsync()
        {
            _cts.Cancel();

            try {
                _listener.Stop();
            }
            catch { /* best-effort */ }

            if (_acceptTask != null) {
                try {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _acceptTask.WaitAsync(timeoutCts.Token);
                }
                catch { /* already recorded via _recordedError or OperationCanceledException */ }
            }

            if (_recordedError != null)
                throw _recordedError;
        }

        public async ValueTask DisposeAsync()
        {
            try {
                await StopAndVerifyAsync();
            }
            catch {
                // Dispose swallows errors; tests must call StopAndVerifyAsync explicitly.
            }

            _cts.Dispose();
        }

        private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken token)
        {
            var offset = 0;
            while (offset < buffer.Length) {
                var n = await stream.ReadAsync(buffer.AsMemory(offset), token);
                if (n == 0)
                    throw new EndOfStreamException("Peer closed the connection unexpectedly");
                offset += n;
            }
        }

        private static async Task<int> ReadAtMostAsync(Stream stream, byte[] buffer, CancellationToken token)
        {
            var offset = 0;
            while (offset < buffer.Length) {
                var n = await stream.ReadAsync(buffer.AsMemory(offset), token);
                if (n == 0)
                    return offset;
                offset += n;
            }
            return offset;
        }
    }
}
