// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H11Client
{
    /// <summary>
    ///     A dedicated (pinned) pool must use exactly one upstream connection so a
    ///     connection-oriented auth handshake stays on a single socket. Recycling happens in
    ///     an async continuation on <c>exchange.Complete</c>, so without serialization a second
    ///     concurrent (or immediately following) exchange dequeues an empty channel and opens a
    ///     second socket. These tests drive <see cref="Http11ConnectionPool.Send" /> directly
    ///     against a loopback server and count the sockets it accepts.
    /// </summary>
    public class DedicatedPoolSingleConnectionTests
    {
        [Fact]
        public async Task Dedicated_pool_serialises_concurrent_sends_onto_one_connection()
        {
            await using var server = CountingHttpServer.Start();

            await using var pool = BuildPool(server.Port, dedicated: true);

            await SendConcurrently(pool, server.Port);

            Assert.Equal(1, server.AcceptedConnections);
        }

        [Fact]
        public async Task Shared_pool_opens_a_connection_per_concurrent_send()
        {
            // Contrast case: the ordinary shared pool has no single-connection lease, so two
            // concurrent sends fan out onto two sockets. This is what made simply dedicating a
            // shared pool to one client insufficient for connection-oriented auth.
            await using var server = CountingHttpServer.Start(gateResponseBodies: true);

            await using var pool = BuildPool(server.Port, dedicated: false);

            await SendConcurrently(pool, server.Port, () => {
                var acceptedConnections = server.AcceptedConnections;
                server.ReleaseResponseBodies();

                Assert.Equal(2, acceptedConnections);
            });

            Assert.Equal(2, server.AcceptedConnections);
        }

        [Fact]
        public async Task Slow_response_body_keeps_pin_active_and_recycles_from_body_completion()
        {
            await using var server = CountingHttpServer.Start(gateResponseBodies: true);

            var setting = ProxyRuntimeSetting.CreateDefault;
            setting.TimeOutSecondsUnusedConnection = 1;

            await using var pool = BuildPool(server.Port, dedicated: true, setting, onFaulted: _ => { });

            var first = MakeExchange(new Authority("127.0.0.1", server.Port, false), server.Port);

            await pool.Send(first, null!, Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32 * 1024),
                new ExchangeScope(), CancellationToken.None);

            Assert.False(first.Complete.IsCompleted,
                "the response body must still own the pinned connection");

            // Exceed the idle timeout while the body is in flight. This must not make the
            // authenticated connection idle or age its eventual recycle timestamp.
            await Task.Delay(TimeSpan.FromMilliseconds(1200));

            pool.LastActivity = DateTime.MinValue;
            Assert.False(pool.TryIdleTeardown(),
                "a pinned pool with an in-flight response body must not idle-evict");

            server.ReleaseResponseBodies();
            await first.Response.Body!.CopyToAsync(Stream.Null);
            await first.Complete;

            var second = MakeExchange(new Authority("127.0.0.1", server.Port, false), server.Port);

            await pool.Send(second, null!, Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32 * 1024),
                new ExchangeScope(), CancellationToken.None);
            await second.Response.Body!.CopyToAsync(Stream.Null);
            await second.Complete;

            Assert.Equal(1, server.AcceptedConnections);
        }

        private static Http11ConnectionPool BuildPool(
            int port,
            bool dedicated,
            ProxyRuntimeSetting? setting = null,
            Action<IHttpConnectionPool>? onFaulted = null)
        {
            var authority = new Authority("127.0.0.1", port, false);
            setting ??= ProxyRuntimeSetting.CreateDefault;

            var builder = new RemoteConnectionBuilder(ITimingProvider.Default, sslConnectionBuilder: null!);

            var dns = new DnsResolutionResult(
                new IPEndPoint(IPAddress.Loopback, port), DateTime.UtcNow, DateTime.UtcNow);

            var pool = new Http11ConnectionPool(
                authority, builder, ITimingProvider.Default, setting,
                setting.ArchiveWriter, dns, onConnectionFaulted: onFaulted, dedicated: dedicated);

            pool.Init();

            return pool;
        }

        private static async Task SendConcurrently(
            Http11ConnectionPool pool, int port, Action? beforeBodyDrain = null)
        {
            var authority = new Authority("127.0.0.1", port, false);

            var exchange1 = MakeExchange(authority, port);
            var exchange2 = MakeExchange(authority, port);

            var task1 = pool.Send(exchange1, null!, Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32 * 1024),
                new ExchangeScope(), CancellationToken.None).AsTask();
            var task2 = pool.Send(exchange2, null!, Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(32 * 1024),
                new ExchangeScope(), CancellationToken.None).AsTask();

            await Task.WhenAll(task1, task2);

            beforeBodyDrain?.Invoke();

            await Task.WhenAll(
                exchange1.Response.Body!.CopyToAsync(Stream.Null),
                exchange2.Response.Body!.CopyToAsync(Stream.Null));

            await exchange1.Complete;
            await exchange2.Complete;
        }

        private static Exchange MakeExchange(Authority authority, int port)
        {
            var request = $"GET / HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\n\r\n";

            return new Exchange(IIdProvider.FromZero, authority, request.AsMemory(), "HTTP/1.1", DateTime.Now);
        }
    }
}
