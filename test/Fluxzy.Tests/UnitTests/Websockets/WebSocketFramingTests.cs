// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Websockets
{
    /// <summary>
    ///     Exercises WsMessage framing through a local Kestrel WS echo server and the
    ///     Fluxzy proxy, asserting that what's captured on disk matches what was sent
    ///     on the wire. Targets two specific parsing bugs:
    ///       - fast-path over-copy when ReadAtLeastAsync returns more than requested
    ///       - slow-path loop bound mixing per-frame vs per-message counters
    /// </summary>
    public class WebSocketFramingTests : IAsyncDisposable
    {
        private readonly string _archiveDirectory;
        private readonly CancellationTokenSource _cts;

        public WebSocketFramingTests()
        {
            _archiveDirectory = Path.Combine(
                Path.GetTempPath(), "fluxzy-ws-framing", Guid.NewGuid().ToString("N"));
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutConstants.Extended));
        }

        public ValueTask DisposeAsync()
        {
            _cts.Dispose();

            try {
                if (Directory.Exists(_archiveDirectory))
                    Directory.Delete(_archiveDirectory, true);
            }
            catch {
                // best effort cleanup
            }

            return default;
        }

        [Fact]
        public async Task SlowPath_Single_Large_Frame_Is_Captured_Intact()
        {
            // Single message > buffered threshold (1024) forces the slow path.
            // Before the fix, the frame-local loop bound was comingled with the
            // cumulative WrittenLength (which was 0 for a fresh message, so this
            // single-frame case happened to work — it's the control baseline).
            var payload = CreateDeterministicPayload(4096, seed: 1);

            var captured = await RoundTrip(ws => SendWhole(ws, payload));

            Assert.Equal(payload.Length, captured.Length);
            Assert.Equal(payload, captured);
        }

        [Fact]
        public async Task SlowPath_Multi_Fragment_Message_Is_Reassembled()
        {
            // Repro for bug #2: multi-fragment message on the slow path.
            // Before the fix, frame 2's loop compared `WrittenLength` (== frame 1
            // length at that point) against the current frame's PayloadLength and
            // exited immediately when frame 2 was smaller than frame 1, dropping
            // the rest of the message.
            var fragments = new[] {
                CreateDeterministicPayload(2048, seed: 10),
                CreateDeterministicPayload(512, seed: 11),   // smaller than frame 1
                CreateDeterministicPayload(1536, seed: 12)
            };

            var expected = ConcatAll(fragments);

            var captured = await RoundTrip(async ws => {
                for (var i = 0; i < fragments.Length; i++) {
                    var isLast = i == fragments.Length - 1;
                    var flags = WebSocketMessageFlags.DisableCompression
                                | (isLast ? WebSocketMessageFlags.EndOfMessage : 0);

                    await ws.SendAsync(fragments[i], WebSocketMessageType.Binary, flags, _cts.Token);
                }
            });

            Assert.Equal(expected.Length, captured.Length);
            Assert.Equal(expected, captured);
        }

        [Fact]
        public async Task FastPath_Small_Message_Is_Captured_Intact()
        {
            // Small single-frame message exercises the fast path (PayloadLength <
            // maxWsMessageLengthBuffered). After the fix, the buffer is sliced to
            // PayloadLength before ToArray, so trailing bytes (if the pipe happens
            // to hold any) can't leak into Data. Triggering coalesce deterministically
            // through ClientWebSocket is flaky (TLS flushes per-send); this test
            // covers the happy path and the WrittenLength/Length invariant.
            var payload = CreateDeterministicPayload(64, seed: 20);

            var captured = await RoundTrip(ws => SendWhole(ws, payload));

            Assert.Equal(payload, captured);
        }

        private static byte[] CreateDeterministicPayload(int length, int seed)
        {
            var buffer = new byte[length];
            var random = new Random(seed);
            random.NextBytes(buffer);
            return buffer;
        }

        private static byte[] ConcatAll(byte[][] parts)
        {
            var total = parts.Sum(p => p.Length);
            var result = new byte[total];
            var offset = 0;
            foreach (var p in parts) {
                Buffer.BlockCopy(p, 0, result, offset, p.Length);
                offset += p.Length;
            }
            return result;
        }

        private async Task SendWhole(ClientWebSocket ws, byte[] payload)
        {
            await ws.SendAsync(payload, WebSocketMessageType.Binary,
                WebSocketMessageFlags.EndOfMessage | WebSocketMessageFlags.DisableCompression,
                _cts.Token);
        }

        /// <summary>
        ///     Runs a single sent-message scenario through the proxy and returns the
        ///     bytes the archive recorded for direction=Sent, reassembling from
        ///     in-memory Data or the on-disk file as appropriate.
        /// </summary>
        private async Task<byte[]> RoundTrip(Func<ClientWebSocket, Task> clientAction)
        {
            var all = await RoundTripCaptureAll(clientAction, expectedSent: 1);
            return all.Single();
        }

        private async Task<byte[][]> RoundTripCaptureAll(
            Func<ClientWebSocket, Task> clientAction, int expectedSent)
        {
            await using var host = await InProcessHost.Create(ConfigureEchoWebSocket, suppressLogging: true);

            var setting = FluxzySetting
                          .CreateLocalRandomPort()
                          .SetOutDirectory(_archiveDirectory);

            setting.ConfigureRule()
                   .WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using (var proxy = new Proxy(setting)) {
                var endpoint = proxy.Run().First();

                using var ws = new ClientWebSocket();
                ws.Options.Proxy = new WebProxy($"http://{endpoint.Address}:{endpoint.Port}");
                ws.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;

                var wsUri = new Uri($"wss://localhost:{host.Port}/ws");
                await ws.ConnectAsync(wsUri, _cts.Token);

                await clientAction(ws);

                // Drain any echoed replies so the server-side copy direction completes
                // cleanly before we close. The upstream path is validated implicitly:
                // if framing were wrong, the echo wouldn't match either — but our
                // assertions target the *captured* representation, not the echo.
                await DrainUntilSent(ws, expectedSent);

                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", _cts.Token);
            }

            using var reader = new DirectoryArchiveReader(_archiveDirectory);
            var exchange = reader.ReadAllExchanges().Single();

            Assert.NotNull(exchange.WebSocketMessages);

            // Filter to data frames only: control frames (Close/Ping/Pong, opcode >= 8)
            // also surface as WsMessage instances.
            var sent = exchange.WebSocketMessages!
                               .Where(m => m.Direction == WsMessageDirection.Sent
                                           && (int) m.OpCode < 8)
                               .OrderBy(m => m.Id)
                               .ToArray();

            Assert.Equal(expectedSent, sent.Length);

            var result = new byte[sent.Length][];
            for (var i = 0; i < sent.Length; i++) {
                var msg = sent[i];
                if (msg.Data != null) {
                    result[i] = msg.Data;
                }
                else {
                    using var stream = reader.GetRequestWebsocketContent(exchange.Id, msg.Id)!;
                    result[i] = await stream.ToArrayGreedyAsync();
                }

                Assert.Equal(msg.Length, msg.WrittenLength);
            }

            return result;
        }

        private async Task DrainUntilSent(ClientWebSocket ws, int expected)
        {
            var buffer = new byte[64 * 1024];
            var got = 0;

            while (got < expected) {
                var res = await ws.ReceiveAsync(buffer, _cts.Token);
                if (res.MessageType == WebSocketMessageType.Close)
                    return;
                if (res.EndOfMessage)
                    got++;
            }
        }

        private static void ConfigureEchoWebSocket(WebApplication app)
        {
            app.UseWebSockets();

            app.Map("/ws", async (HttpContext ctx) => {
                if (!ctx.WebSockets.IsWebSocketRequest) {
                    ctx.Response.StatusCode = 400;
                    return;
                }

                using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
                var buffer = new byte[64 * 1024];

                while (ws.State == WebSocketState.Open) {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult res;
                    do {
                        res = await ws.ReceiveAsync(buffer, ctx.RequestAborted);
                        if (res.MessageType == WebSocketMessageType.Close) {
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ctx.RequestAborted);
                            return;
                        }
                        ms.Write(buffer, 0, res.Count);
                    } while (!res.EndOfMessage);

                    var payload = ms.ToArray();
                    await ws.SendAsync(payload, res.MessageType, true, ctx.RequestAborted);
                }
            });
        }
    }
}
