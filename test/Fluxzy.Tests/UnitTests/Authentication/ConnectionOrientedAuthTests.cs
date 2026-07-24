// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Authentication
{
    /// <summary>
    ///     Connection-oriented authentication (NTLM / Negotiate-Kerberos as done by SSPI on
    ///     Windows) authenticates the TCP connection, not the request. Fluxzy supports it by
    ///     pinning the upstream connection to the originating downstream connection: the
    ///     multi-leg handshake of one client rides a single upstream connection, and that
    ///     connection is never handed to another client.
    ///
    ///     <see cref="NtlmLikeTcpServer" /> emulates the server-side semantics; these tests
    ///     drive the handshake through Fluxzy as an explicit plain proxy, which exercises the
    ///     same Http11ConnectionPool path as an HTTPS MITM exchange. Pinning is on by default
    ///     (built-in rule matching NTLM/Negotiate credentials), so no rule wiring is needed here.
    /// </summary>
    public class ConnectionOrientedAuthTests
    {
        [Fact]
        public async Task Ntlm_handshake_completes_for_a_single_client()
        {
            await using var server = NtlmLikeTcpServer.Start();
            await using var proxy = new Proxy(FluxzySetting.CreateLocalRandomPort());

            var endPoint = proxy.Run().First();
            using var client = CreateProxiedClient(endPoint);

            var url = $"http://127.0.0.1:{server.Port}/resource";

            var leg1 = await Send(client, url, null);
            Assert.Equal(HttpStatusCode.Unauthorized, leg1.Status);
            Assert.Equal("NTLM", leg1.WwwAuthenticate);

            var leg2 = await Send(client, url, "NTLM TYPE1");
            Assert.Equal(HttpStatusCode.Unauthorized, leg2.Status);
            Assert.StartsWith("NTLM CH-", leg2.WwwAuthenticate);

            var challenge = leg2.WwwAuthenticate!.Substring("NTLM ".Length);

            var leg3 = await Send(client, url, $"NTLM TYPE3 {challenge} alice");

            Assert.True(
                leg3.Status == HttpStatusCode.OK,
                $"TYPE3 leg was rejected: the challenge and the TYPE3 landed on different " +
                $"upstream connections. Server log: {DumpLog(server)}");

            Assert.Contains("user=alice", leg3.Body);
        }

        [Fact]
        public async Task Ntlm_handshake_is_isolated_from_a_concurrent_client()
        {
            await using var server = NtlmLikeTcpServer.Start();
            await using var proxy = new Proxy(FluxzySetting.CreateLocalRandomPort());

            var endPoint = proxy.Run().First();

            using var clientA = CreateProxiedClient(endPoint);
            using var clientB = CreateProxiedClient(endPoint);

            var url = $"http://127.0.0.1:{server.Port}/resource";

            await Send(clientA, url, null);
            var challengeLeg = await Send(clientA, url, "NTLM TYPE1");

            Assert.Equal(HttpStatusCode.Unauthorized, challengeLeg.Status);
            Assert.StartsWith("NTLM CH-", challengeLeg.WwwAuthenticate);

            var challenge = challengeLeg.WwwAuthenticate!.Substring("NTLM ".Length);

            // an unrelated client hits the same host while A's handshake is half-open
            await Send(clientB, url, null);

            var finalLeg = await Send(clientA, url, $"NTLM TYPE3 {challenge} alice");

            Assert.True(
                finalLeg.Status == HttpStatusCode.OK,
                $"Client B's request destroyed client A's half-authenticated security context. " +
                $"Server log: {DumpLog(server)}");

            Assert.Contains("user=alice", finalLeg.Body);
        }

        [Fact]
        public async Task Authenticated_connection_stays_sticky_for_followup_requests()
        {
            await using var server = NtlmLikeTcpServer.Start();
            await using var proxy = new Proxy(FluxzySetting.CreateLocalRandomPort());

            var endPoint = proxy.Run().First();
            using var client = CreateProxiedClient(endPoint);

            var url = $"http://127.0.0.1:{server.Port}/resource";

            await Send(client, url, null);
            var challengeLeg = await Send(client, url, "NTLM TYPE1");
            var challenge = challengeLeg.WwwAuthenticate!.Substring("NTLM ".Length);
            var authenticated = await Send(client, url, $"NTLM TYPE3 {challenge} alice");

            Assert.Equal(HttpStatusCode.OK, authenticated.Status);

            // Subsequent requests carry no Authorization header: the connection itself is the
            // credential, so the pin must persist and keep returning the authenticated identity.
            // Fired repeatedly on purpose: the recycle of the pinned connection happens in an
            // async continuation, so a single follow-up only catches the race intermittently.
            for (var i = 0; i < 25; i++) {
                var followUp = await Send(client, url, null);

                Assert.True(
                    followUp.Status == HttpStatusCode.OK && followUp.Body.Contains("user=alice"),
                    $"Follow-up #{i} was not served over the pinned authenticated connection " +
                    $"(status {followUp.Status}, body '{followUp.Body}'). A fresh upstream connection " +
                    $"was opened instead of reusing the pinned one. Server log: {DumpLog(server)}");
            }
        }

        [Fact]
        public async Task Authenticated_connection_is_not_reused_for_another_client()
        {
            await using var server = NtlmLikeTcpServer.Start();
            await using var proxy = new Proxy(FluxzySetting.CreateLocalRandomPort());

            var endPoint = proxy.Run().First();

            using var clientA = CreateProxiedClient(endPoint);
            using var clientB = CreateProxiedClient(endPoint);

            var url = $"http://127.0.0.1:{server.Port}/resource";

            await Send(clientA, url, null);
            var challengeLeg = await Send(clientA, url, "NTLM TYPE1");
            var challenge = challengeLeg.WwwAuthenticate!.Substring("NTLM ".Length);
            var authenticated = await Send(clientA, url, $"NTLM TYPE3 {challenge} alice");

            Assert.Equal(HttpStatusCode.OK, authenticated.Status);

            // let the pool have a chance to recycle A's connection
            await Task.Delay(150);

            var response = await Send(clientB, url, null);

            Assert.True(
                response.Status == HttpStatusCode.Unauthorized,
                $"Client B inherited client A's authenticated identity ({response.Body}). " +
                $"Server log: {DumpLog(server)}");
        }

        [Fact]
        public async Task Pinned_connection_death_mid_handshake_restarts_cleanly()
        {
            await using var server = NtlmLikeTcpServer.Start(dropOnceAfterChallenge: true);
            await using var proxy = new Proxy(FluxzySetting.CreateLocalRandomPort());

            var endPoint = proxy.Run().First();
            using var client = CreateProxiedClient(endPoint);

            var url = $"http://127.0.0.1:{server.Port}/resource";

            await Send(client, url, null);
            var challengeLeg = await Send(client, url, "NTLM TYPE1");
            var challenge = challengeLeg.WwwAuthenticate!.Substring("NTLM ".Length);

            // the pinned connection was dropped by the server after this challenge; the TYPE3
            // lands on a fresh connection that holds no context, so the server restarts the
            // handshake. The proxy must surface a clean 401, not an exception or a hang.
            var afterDeath = await Send(client, url, $"NTLM TYPE3 {challenge} alice");
            Assert.Equal(HttpStatusCode.Unauthorized, afterDeath.Status);

            // and a fresh handshake on the (now healthy) pinned connection completes
            var retryChallenge = await Send(client, url, "NTLM TYPE1");
            var retry = await Send(client, url,
                $"NTLM TYPE3 {retryChallenge.WwwAuthenticate!.Substring("NTLM ".Length)} alice");

            Assert.True(
                retry.Status == HttpStatusCode.OK,
                $"Handshake did not recover after the pinned connection died. " +
                $"Server log: {DumpLog(server)}");
        }

        [Fact]
        public async Task Identity_never_leaks_when_pinning_is_disabled()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetDisableAutomaticConnectionAuthPinning(true);

            await using var server = NtlmLikeTcpServer.Start();
            await using var proxy = new Proxy(setting);

            var endPoint = proxy.Run().First();

            using var clientA = CreateProxiedClient(endPoint);
            using var clientB = CreateProxiedClient(endPoint);

            var url = $"http://127.0.0.1:{server.Port}/resource";

            // client A runs a full handshake. Without pinning it is not expected to succeed,
            // but whatever happens, an unauthenticated client B must never see A's identity.
            await Send(clientA, url, null);
            var challengeLeg = await Send(clientA, url, "NTLM TYPE1");

            if (challengeLeg.WwwAuthenticate?.StartsWith("NTLM CH-") == true) {
                var challenge = challengeLeg.WwwAuthenticate.Substring("NTLM ".Length);
                await Send(clientA, url, $"NTLM TYPE3 {challenge} alice");
            }

            await Task.Delay(150);

            var response = await Send(clientB, url, null);

            Assert.DoesNotContain("user=alice", response.Body);
        }

        private static HttpClient CreateProxiedClient(IPEndPoint proxyEndPoint)
        {
            return new HttpClient(new HttpClientHandler {
                Proxy = new WebProxy($"http://127.0.0.1:{proxyEndPoint.Port}"),
                UseProxy = true,
                MaxConnectionsPerServer = 1
            }) {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        private static async Task<(HttpStatusCode Status, string? WwwAuthenticate, string Body)> Send(
            HttpClient client, string url, string? authorization)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (authorization != null)
                request.Headers.TryAddWithoutValidation("Authorization", authorization);

            using var response = await client.SendAsync(request);

            var wwwAuthenticate = response.Headers.TryGetValues("WWW-Authenticate", out var values)
                ? values.First()
                : null;

            var body = await response.Content.ReadAsStringAsync();

            return (response.StatusCode, wwwAuthenticate, body);
        }

        private static string DumpLog(NtlmLikeTcpServer server)
        {
            return string.Join(" | ", server.RequestLog);
        }
    }
}
