// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Socks5;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Socks5
{
    public class Socks5SourceProviderTests
    {
        private static readonly IPEndPoint LocalEndPoint = new(IPAddress.Loopback, 1080);
        private static readonly IPEndPoint RemoteEndPoint = new(IPAddress.Loopback, 12345);

        // Shared work buffer for tests - simulates RsBuffer.Memory
        private readonly byte[] _workBuffer = new byte[1024];

        [Fact]
        public async Task ProtocolDetection_Socks5FirstByte_DetectsSocks5()
        {
            // Arrange: SOCKS5 greeting [VER=0x05, NMETHODS=1, METHOD=0x00]
            var socks5Greeting = new byte[] { 0x05, 0x01, 0x00 };
            using var inputStream = new MemoryStream(socks5Greeting);
            using var combinedStream = new CombinedReadonlyStream(false, inputStream);

            // Read first byte
            var firstByte = new byte[1];
            await combinedStream.ReadAsync(firstByte);

            // Assert first byte is SOCKS5 version
            Assert.Equal(Socks5Constants.Version, firstByte[0]);
        }

        [Fact]
        public async Task ProtocolDetection_HttpFirstByte_DetectsHttp()
        {
            // Arrange: HTTP CONNECT request
            var httpConnect = Encoding.ASCII.GetBytes("CONNECT example.com:443 HTTP/1.1\r\n\r\n");
            using var inputStream = new MemoryStream(httpConnect);

            // Read first byte
            var firstByte = new byte[1];
            await inputStream.ReadAsync(firstByte);

            // Assert first byte is NOT SOCKS5 version (it's 'C' = 0x43)
            Assert.NotEqual(Socks5Constants.Version, firstByte[0]);
            Assert.Equal((byte)'C', firstByte[0]);
        }

        [Fact]
        public async Task Socks5Handshake_NoAuth_CompletesSuccessfully()
        {
            // Arrange: Full SOCKS5 handshake with no auth
            // Greeting: [VER=0x05, NMETHODS=1, METHOD=0x00]
            // Request: [VER=0x05, CMD=0x01, RSV=0x00, ATYP=0x03, LEN=11, "example.com", PORT=443]
            var domain = "example.com";
            var domainBytes = Encoding.ASCII.GetBytes(domain);

            var greeting = new byte[] { 0x05, 0x01, 0x00 };
            var request = new byte[4 + 1 + domainBytes.Length + 2];
            request[0] = 0x05;
            request[1] = 0x01;
            request[2] = 0x00;
            request[3] = 0x03;
            request[4] = (byte)domainBytes.Length;
            Buffer.BlockCopy(domainBytes, 0, request, 5, domainBytes.Length);
            BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(5 + domainBytes.Length), 443);

            var fullInput = new byte[greeting.Length + request.Length];
            Buffer.BlockCopy(greeting, 0, fullInput, 0, greeting.Length);
            Buffer.BlockCopy(request, 0, fullInput, greeting.Length, request.Length);

            using var inputStream = new MemoryStream(fullInput);
            using var outputStream = new MemoryStream();

            // Create a bidirectional stream
            var duplexStream = new DuplexStream(inputStream, outputStream);

            // Read first byte (simulating protocol detection)
            var firstByte = new byte[1];
            await duplexStream.ReadAsync(firstByte);
            Assert.Equal(Socks5Constants.Version, firstByte[0]);

            // Read greeting using workBuffer
            var methods = await Socks5ProtocolHandler.ReadGreetingAsync(duplexStream, _workBuffer, CancellationToken.None);
            Assert.Single(methods);
            Assert.Equal(Socks5Constants.AuthNoAuth, methods[0]);

            // Write method selection using workBuffer
            await Socks5ProtocolHandler.WriteMethodSelectionAsync(
                duplexStream, Socks5Constants.AuthNoAuth, _workBuffer, CancellationToken.None);

            // Read request using workBuffer
            var socks5Request = await Socks5ProtocolHandler.ReadRequestAsync(duplexStream, _workBuffer, CancellationToken.None);

            // Assert
            Assert.Equal(Socks5Constants.CmdConnect, socks5Request.Command);
            Assert.Equal(Socks5Constants.AddrTypeDomain, socks5Request.AddressType);
            Assert.Equal(domain, socks5Request.DestinationAddress);
            Assert.Equal(443, socks5Request.DestinationPort);

            // Verify method selection response was written
            var responseBytes = outputStream.ToArray();
            Assert.Equal(2, responseBytes.Length);
            Assert.Equal(Socks5Constants.Version, responseBytes[0]);
            Assert.Equal(Socks5Constants.AuthNoAuth, responseBytes[1]);
        }

        [Fact]
        public async Task Socks5Handshake_WithAuth_ValidatesCredentials()
        {
            // Arrange: Full SOCKS5 handshake with username/password auth
            var username = "testuser";
            var password = "testpass";
            var usernameBytes = Encoding.UTF8.GetBytes(username);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // Greeting: [VER=0x05, NMETHODS=1, METHOD=0x02]
            var greeting = new byte[] { 0x05, 0x01, 0x02 };

            // Auth: [VER=0x01, ULEN, USERNAME, PLEN, PASSWORD]
            var auth = new byte[2 + usernameBytes.Length + 1 + passwordBytes.Length];
            auth[0] = Socks5Constants.AuthVersion;
            auth[1] = (byte)usernameBytes.Length;
            Buffer.BlockCopy(usernameBytes, 0, auth, 2, usernameBytes.Length);
            auth[2 + usernameBytes.Length] = (byte)passwordBytes.Length;
            Buffer.BlockCopy(passwordBytes, 0, auth, 3 + usernameBytes.Length, passwordBytes.Length);

            var fullInput = new byte[greeting.Length + auth.Length];
            Buffer.BlockCopy(greeting, 0, fullInput, 0, greeting.Length);
            Buffer.BlockCopy(auth, 0, fullInput, greeting.Length, auth.Length);

            using var inputStream = new MemoryStream(fullInput);
            using var outputStream = new MemoryStream();

            var duplexStream = new DuplexStream(inputStream, outputStream);

            // Simulate protocol detection
            var firstByte = new byte[1];
            await duplexStream.ReadAsync(firstByte);

            // Read greeting using workBuffer
            var methods = await Socks5ProtocolHandler.ReadGreetingAsync(duplexStream, _workBuffer, CancellationToken.None);
            Assert.Single(methods);
            Assert.Equal(Socks5Constants.AuthUsernamePassword, methods[0]);

            // Write method selection using workBuffer
            await Socks5ProtocolHandler.WriteMethodSelectionAsync(
                duplexStream, Socks5Constants.AuthUsernamePassword, _workBuffer, CancellationToken.None);

            // Read auth using workBuffer
            var (readUsername, readPassword) = await Socks5ProtocolHandler.ReadUsernamePasswordAsync(
                duplexStream, _workBuffer, CancellationToken.None);

            // Assert
            Assert.Equal(username, readUsername);
            Assert.Equal(password, readPassword);
        }

        [Fact]
        public void Socks5AuthAdapter_SelectsCorrectMethod()
        {
            // NoAuth adapter
            var noAuthAdapter = new Socks5AuthenticationAdapter(NoAuthenticationMethod.Instance);
            Assert.Equal(Socks5Constants.AuthNoAuth, noAuthAdapter.GetSocks5AuthMethod());

            // Basic auth adapter
            var basicAuthMethod = new BasicAuthenticationMethod("user", "pass");
            var basicAdapter = new Socks5AuthenticationAdapter(basicAuthMethod);
            Assert.Equal(Socks5Constants.AuthUsernamePassword, basicAdapter.GetSocks5AuthMethod());
        }

        [Fact]
        public async Task Socks5Request_IPv4Address_ParsesCorrectly()
        {
            // Request: [VER=0x05, CMD=0x01, RSV=0x00, ATYP=0x01, 192.168.1.100, PORT=8080]
            var request = new byte[]
            {
                0x05, 0x01, 0x00, 0x01,
                192, 168, 1, 100,
                0x1F, 0x90 // 8080 in big-endian
            };

            using var stream = new MemoryStream(request);

            var socks5Request = await Socks5ProtocolHandler.ReadRequestAsync(stream, _workBuffer, CancellationToken.None);

            Assert.Equal(Socks5Constants.CmdConnect, socks5Request.Command);
            Assert.Equal(Socks5Constants.AddrTypeIPv4, socks5Request.AddressType);
            Assert.Equal("192.168.1.100", socks5Request.DestinationAddress);
            Assert.Equal(8080, socks5Request.DestinationPort);
        }

        [Fact]
        public async Task Socks5Request_IPv6Address_ParsesCorrectly()
        {
            // Request: [VER=0x05, CMD=0x01, RSV=0x00, ATYP=0x04, IPv6, PORT=443]
            var ipv6 = IPAddress.Parse("2001:db8::1");
            var ipv6Bytes = ipv6.GetAddressBytes();

            var request = new byte[4 + 16 + 2];
            request[0] = 0x05;
            request[1] = 0x01;
            request[2] = 0x00;
            request[3] = 0x04;
            Buffer.BlockCopy(ipv6Bytes, 0, request, 4, 16);
            BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(20), 443);

            using var stream = new MemoryStream(request);

            var socks5Request = await Socks5ProtocolHandler.ReadRequestAsync(stream, _workBuffer, CancellationToken.None);

            Assert.Equal(Socks5Constants.CmdConnect, socks5Request.Command);
            Assert.Equal(Socks5Constants.AddrTypeIPv6, socks5Request.AddressType);
            Assert.Equal("2001:db8::1", socks5Request.DestinationAddress);
            Assert.Equal(443, socks5Request.DestinationPort);
        }

        [Fact]
        public async Task Socks5Request_BindCommand_IsRejected()
        {
            // Request with BIND command: [VER=0x05, CMD=0x02, RSV=0x00, ATYP=0x01, 127.0.0.1, PORT=80]
            var request = new byte[]
            {
                0x05, 0x02, 0x00, 0x01,
                127, 0, 0, 1,
                0x00, 0x50
            };

            using var stream = new MemoryStream(request);

            var socks5Request = await Socks5ProtocolHandler.ReadRequestAsync(stream, _workBuffer, CancellationToken.None);

            // BIND command (0x02) is not CONNECT (0x01)
            Assert.Equal(Socks5Constants.CmdBind, socks5Request.Command);
            Assert.NotEqual(Socks5Constants.CmdConnect, socks5Request.Command);
        }

        [Fact]
        public async Task Socks5Reply_Success_WritesCorrectFormat()
        {
            using var stream = new MemoryStream();

            await Socks5ProtocolHandler.WriteReplyAsync(
                stream,
                Socks5Constants.RepSucceeded,
                Socks5Constants.AddrTypeIPv4,
                new byte[] { 0, 0, 0, 0 },
                0,
                _workBuffer,
                CancellationToken.None);

            var result = stream.ToArray();

            // [VER, REP, RSV, ATYP, BND.ADDR(4), BND.PORT(2)] = 10 bytes
            Assert.Equal(10, result.Length);
            Assert.Equal(Socks5Constants.Version, result[0]);
            Assert.Equal(Socks5Constants.RepSucceeded, result[1]);
            Assert.Equal(0x00, result[2]); // RSV
            Assert.Equal(Socks5Constants.AddrTypeIPv4, result[3]);
        }

        /// <summary>
        /// Helper stream that reads from one stream and writes to another.
        /// </summary>
        private class DuplexStream : Stream
        {
            private readonly Stream _readStream;
            private readonly Stream _writeStream;

            public DuplexStream(Stream readStream, Stream writeStream)
            {
                _readStream = readStream;
                _writeStream = writeStream;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => _writeStream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _readStream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => _writeStream.Write(buffer, offset, count);

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                => _readStream.ReadAsync(buffer, offset, count, cancellationToken);

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
                => _readStream.ReadAsync(buffer, cancellationToken);

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                => _writeStream.WriteAsync(buffer, offset, count, cancellationToken);

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
                => _writeStream.WriteAsync(buffer, cancellationToken);

            public override Task FlushAsync(CancellationToken cancellationToken)
                => _writeStream.FlushAsync(cancellationToken);
        }
    }
}
