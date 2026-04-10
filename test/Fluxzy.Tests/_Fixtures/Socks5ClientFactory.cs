using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests._Fixtures
{
    /// <summary>
    ///     Creates HttpClient instances that route traffic through a proxy via SOCKS5.
    /// </summary>
    public static class Socks5ClientFactory
    {
        public static HttpClient Create(
            IPEndPoint proxyEndPoint, int timeoutSeconds = 15,
            Version? httpVersion = null,
            Func<Stream, Stream>? streamWrapper = null)
        {
            var normalized = NormalizeEndPoint(proxyEndPoint);
            var useH2 = httpVersion != null && httpVersion.Major >= 2;

            var sslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            };

            if (useH2) {
                sslOptions.ApplicationProtocols = new System.Collections.Generic.List<SslApplicationProtocol>
                {
                    SslApplicationProtocol.Http2,
                    SslApplicationProtocol.Http11
                };
            }

            var handler = new SocketsHttpHandler
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(normalized.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(normalized, cancellationToken);

                    Stream stream = new NetworkStream(socket, ownsSocket: true);

                    if (streamWrapper != null)
                        stream = streamWrapper(stream);

                    await PerformSocks5HandshakeAsync(stream, context.DnsEndPoint, cancellationToken);

                    return stream;
                },
                SslOptions = sslOptions
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            if (httpVersion != null) {
                client.DefaultRequestVersion = httpVersion;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
            }

            return client;
        }

        private static IPEndPoint NormalizeEndPoint(IPEndPoint endPoint)
        {
            if (endPoint.Address.Equals(IPAddress.Any))
                return new IPEndPoint(IPAddress.Loopback, endPoint.Port);

            if (endPoint.Address.Equals(IPAddress.IPv6Any))
                return new IPEndPoint(IPAddress.IPv6Loopback, endPoint.Port);

            return endPoint;
        }

        public static async Task PerformSocks5HandshakeAsync(
            Stream stream, DnsEndPoint target, CancellationToken cancellationToken)
        {
            // 1. Greeting: version 5, 1 method (no auth)
            await stream.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            // 2. Read method selection
            var methodResponse = new byte[2];
            await ReadExactAsync(stream, methodResponse, cancellationToken);

            if (methodResponse[0] != 0x05)
                throw new InvalidOperationException($"Invalid SOCKS version: {methodResponse[0]}");

            if (methodResponse[1] != 0x00)
                throw new InvalidOperationException($"Server rejected no-auth method: {methodResponse[1]}");

            // 3. Send CONNECT request (domain name type)
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

            // 4. Read CONNECT reply
            var replyHeader = new byte[4];
            await ReadExactAsync(stream, replyHeader, cancellationToken);

            if (replyHeader[0] != 0x05)
                throw new InvalidOperationException($"Invalid SOCKS version in reply: {replyHeader[0]}");

            if (replyHeader[1] != 0x00)
                throw new InvalidOperationException($"SOCKS5 connection failed with code: {replyHeader[1]}");

            // Read bound address based on type
            int addrLen = replyHeader[3] switch
            {
                0x01 => 4,  // IPv4
                0x03 => (await ReadByteAsync(stream, cancellationToken)), // Domain
                0x04 => 16, // IPv6
                _ => throw new InvalidOperationException($"Unknown address type: {replyHeader[3]}")
            };

            var remaining = new byte[addrLen + 2]; // address + port
            await ReadExactAsync(stream, remaining, cancellationToken);
        }

        private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
        {
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(totalRead), ct);
                if (read == 0)
                    throw new IOException("Connection closed unexpectedly");
                totalRead += read;
            }
        }

        private static async Task<byte> ReadByteAsync(Stream stream, CancellationToken ct)
        {
            var buffer = new byte[1];
            await ReadExactAsync(stream, buffer, ct);
            return buffer[0];
        }
    }
}
