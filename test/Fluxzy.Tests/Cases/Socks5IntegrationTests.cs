// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class Socks5IntegrationTests
    {
        [Fact]
        public async Task Socks5_NoAuth_HttpRequest_Succeeds()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Socks5-Test", "success"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = GetValidEndPoint(endPoints);

            // Act
            using var client = CreateSocks5HttpClient(proxyEndPoint, setting);
            var response = await client.GetAsync(TestConstants.Http11Host + "/ip");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.TryGetValues("X-Socks5-Test", out var values));
            Assert.Equal("success", values?.First());
        }

        [Fact]
        public async Task Socks5_NoAuth_HttpsRequest_Succeeds()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Socks5-Https", "secured"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = GetValidEndPoint(endPoints);

            // Act
            using var client = CreateSocks5HttpClient(proxyEndPoint, setting);
            var response = await client.GetAsync(TestConstants.Http2Host + "/ip");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.TryGetValues("X-Socks5-Https", out var values));
            Assert.Equal("secured", values?.First());
        }

        [Fact]
        public async Task Socks5_WithBasicAuth_ValidCredentials_Succeeds()
        {
            // Arrange
            var username = "testuser";
            var password = "testpass";

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetProxyAuthentication(ProxyAuthentication.Basic(username, password));
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Socks5-Auth", "authenticated"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = GetValidEndPoint(endPoints);

            // Act
            using var client = CreateSocks5HttpClient(proxyEndPoint, setting, username, password);
            var response = await client.GetAsync(TestConstants.Http11Host + "/ip");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.TryGetValues("X-Socks5-Auth", out var values));
            Assert.Equal("authenticated", values?.First());
        }

        [Fact]
        public async Task Socks5_WithBasicAuth_InvalidCredentials_Fails()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetProxyAuthentication(ProxyAuthentication.Basic("correctuser", "correctpass"));

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = GetValidEndPoint(endPoints);

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                using var client = CreateSocks5HttpClient(proxyEndPoint, setting, "wronguser", "wrongpass");
                await client.GetAsync(TestConstants.Http11Host + "/ip");
            });
        }

        [Fact]
        public async Task Socks5_MultipleRequests_SameConnection_Succeeds()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Request-Count", "ok"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = GetValidEndPoint(endPoints);

            using var client = CreateSocks5HttpClient(proxyEndPoint, setting);

            // Act - Make multiple requests to the same endpoint
            var response1 = await client.GetAsync(TestConstants.Http11Host + "/ip");
            var response2 = await client.GetAsync(TestConstants.Http11Host + "/ip");
            var response3 = await client.GetAsync(TestConstants.Http11Host + "/ip");

            // Assert
            Assert.True(response1.IsSuccessStatusCode, $"Response1 failed with status {response1.StatusCode}");
            Assert.True(response2.IsSuccessStatusCode, $"Response2 failed with status {response2.StatusCode}");
            Assert.True(response3.IsSuccessStatusCode, $"Response3 failed with status {response3.StatusCode}");
        }

        [Fact]
        public async Task Socks5_PostRequest_WithBody_Succeeds()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = GetValidEndPoint(endPoints);

            using var client = CreateSocks5HttpClient(proxyEndPoint, setting);
            var content = new StringContent("{\"test\":\"data\"}", Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(TestConstants.Http11Host + "/post", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Socks5_RulesApply_ResponseModified()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.ConfigureRule()
                .WhenHostMatch("sandbox.fluxzy.io")
                .Do(new AddResponseHeaderAction("X-Custom-Header", "custom-value"));

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = GetValidEndPoint(endPoints);

            using var client = CreateSocks5HttpClient(proxyEndPoint, setting);

            // Act
            var response = await client.GetAsync(TestConstants.Http11Host + "/ip");

            // Assert
            Assert.True(response.Headers.TryGetValues("X-Custom-Header", out var values));
            Assert.Equal("custom-value", values?.First());
        }

        [Fact]
        public async Task Socks5_PlainHttp_NoTls_Succeeds()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Plain-Http", "plain"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = GetValidEndPoint(endPoints);

            using var client = CreateSocks5HttpClient(proxyEndPoint, setting);

            // Act
            var response = await client.GetAsync(TestConstants.PlainHttp11 + "/ip");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.TryGetValues("X-Plain-Http", out var values));
            Assert.Equal("plain", values?.First());
        }

        [Fact]
        public async Task Socks5_ProtocolDetection_HttpStillWorks()
        {
            // Arrange - Verify that HTTP CONNECT still works with ProtocolDetectingSourceProvider
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Http-Test", "http-works"),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();

            // Act - Use regular HTTP proxy client (not SOCKS5)
            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);
            var response = await client.GetAsync(TestConstants.Http11Host + "/ip");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.TryGetValues("X-Http-Test", out var values));
            Assert.Equal("http-works", values?.First());
        }

        [Fact]
        public async Task Socks5_ConcurrentRequests_AllSucceed()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = GetValidEndPoint(endPoints);

            // Act - Make concurrent requests
            var tasks = Enumerable.Range(0, 5).Select(async i =>
            {
                using var client = CreateSocks5HttpClient(proxyEndPoint, setting);
                return await client.GetAsync(TestConstants.Http11Host + "/ip");
            });

            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));
        }

        private static IPEndPoint GetValidEndPoint(IReadOnlyCollection<IPEndPoint> endPoints)
        {
            var endPoint = endPoints.First();

            if (endPoint.Address.Equals(IPAddress.Any))
                return new IPEndPoint(IPAddress.Loopback, endPoint.Port);

            if (endPoint.Address.Equals(IPAddress.IPv6Any))
                return new IPEndPoint(IPAddress.IPv6Loopback, endPoint.Port);

            return endPoint;
        }

        private static HttpClient CreateSocks5HttpClient(
            IPEndPoint proxyEndPoint,
            FluxzySetting setting,
            string? username = null,
            string? password = null)
        {
            var thumbPrint = setting.CaCertificate.GetX509Certificate().Thumbprint;

            var handler = new SocketsHttpHandler
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(proxyEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(proxyEndPoint, cancellationToken);

                    var stream = new NetworkStream(socket, ownsSocket: true);

                    // Perform SOCKS5 handshake
                    await PerformSocks5Handshake(stream, context.DnsEndPoint, username, password, cancellationToken);

                    return stream;
                },
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, chain, _) =>
                    {
                        if (chain == null || chain.ChainElements.Count < 1)
                            return false;

                        var lastChainThumbPrint =
                            chain.ChainElements[chain.ChainElements.Count - 1].Certificate.Thumbprint;

                        return lastChainThumbPrint == thumbPrint;
                    }
                }
            };

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private static async Task PerformSocks5Handshake(
            Stream stream,
            DnsEndPoint target,
            string? username,
            string? password,
            CancellationToken cancellationToken)
        {
            // 1. Send greeting
            byte[] greeting;
            if (username != null && password != null)
            {
                // Support both NoAuth and UsernamePassword
                greeting = new byte[] { 0x05, 0x02, 0x00, 0x02 };
            }
            else
            {
                // Only NoAuth
                greeting = new byte[] { 0x05, 0x01, 0x00 };
            }

            await stream.WriteAsync(greeting, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            // 2. Read method selection
            var methodResponse = new byte[2];
            await ReadExactAsync(stream, methodResponse, cancellationToken);

            if (methodResponse[0] != 0x05)
                throw new InvalidOperationException($"Invalid SOCKS version: {methodResponse[0]}");

            if (methodResponse[1] == 0xFF)
                throw new InvalidOperationException("No acceptable authentication method");

            // 3. Handle authentication if required
            if (methodResponse[1] == 0x02)
            {
                if (username == null || password == null)
                    throw new InvalidOperationException("Server requires authentication but no credentials provided");

                var usernameBytes = Encoding.UTF8.GetBytes(username);
                var passwordBytes = Encoding.UTF8.GetBytes(password);

                var authRequest = new byte[3 + usernameBytes.Length + passwordBytes.Length];
                authRequest[0] = 0x01; // Auth version
                authRequest[1] = (byte)usernameBytes.Length;
                Buffer.BlockCopy(usernameBytes, 0, authRequest, 2, usernameBytes.Length);
                authRequest[2 + usernameBytes.Length] = (byte)passwordBytes.Length;
                Buffer.BlockCopy(passwordBytes, 0, authRequest, 3 + usernameBytes.Length, passwordBytes.Length);

                await stream.WriteAsync(authRequest, cancellationToken);
                await stream.FlushAsync(cancellationToken);

                var authResponse = new byte[2];
                await ReadExactAsync(stream, authResponse, cancellationToken);

                if (authResponse[1] != 0x00)
                    throw new InvalidOperationException("Authentication failed");
            }

            // 4. Send connect request
            var hostBytes = Encoding.ASCII.GetBytes(target.Host);
            var request = new byte[4 + 1 + hostBytes.Length + 2];
            request[0] = 0x05; // Version
            request[1] = 0x01; // CONNECT
            request[2] = 0x00; // Reserved
            request[3] = 0x03; // Domain name
            request[4] = (byte)hostBytes.Length;
            Buffer.BlockCopy(hostBytes, 0, request, 5, hostBytes.Length);
            BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(5 + hostBytes.Length), (ushort)target.Port);

            await stream.WriteAsync(request, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            // 5. Read reply
            var replyHeader = new byte[4];
            await ReadExactAsync(stream, replyHeader, cancellationToken);

            if (replyHeader[0] != 0x05)
                throw new InvalidOperationException($"Invalid SOCKS version in reply: {replyHeader[0]}");

            if (replyHeader[1] != 0x00)
                throw new InvalidOperationException($"SOCKS5 connection failed with code: {replyHeader[1]}");

            // Read the rest of the reply based on address type
            var addrType = replyHeader[3];
            int addrLen = addrType switch
            {
                0x01 => 4,  // IPv4
                0x03 => await ReadByteAsync(stream, cancellationToken), // Domain (first byte is length)
                0x04 => 16, // IPv6
                _ => throw new InvalidOperationException($"Unknown address type: {addrType}")
            };

            var remaining = new byte[addrLen + 2]; // Address + port
            await ReadExactAsync(stream, remaining, cancellationToken);

            // Connection established!
        }

        private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
                if (read == 0)
                    throw new IOException("Connection closed unexpectedly");
                totalRead += read;
            }
            return totalRead;
        }

        private static async Task<byte> ReadByteAsync(Stream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[1];
            await ReadExactAsync(stream, buffer, cancellationToken);
            return buffer[0];
        }
    }
}
