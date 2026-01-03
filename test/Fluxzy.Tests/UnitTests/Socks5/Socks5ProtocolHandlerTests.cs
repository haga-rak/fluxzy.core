// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core.Socks5;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Socks5
{
    public class Socks5ProtocolHandlerTests
    {
        [Fact]
        public async Task ReadGreeting_SingleNoAuthMethod_ReturnsSingleMethod()
        {
            // Arrange: [NMETHODS=1, METHOD=0x00]
            var data = new byte[] { 0x01, 0x00 };
            using var stream = new MemoryStream(data);

            // Act
            var methods = await Socks5ProtocolHandler.ReadGreetingAsync(stream, CancellationToken.None);

            // Assert
            Assert.Single(methods);
            Assert.Equal(Socks5Constants.AuthNoAuth, methods[0]);
        }

        [Fact]
        public async Task ReadGreeting_MultipleMethods_ReturnsAllMethods()
        {
            // Arrange: [NMETHODS=2, METHOD=0x00, METHOD=0x02]
            var data = new byte[] { 0x02, 0x00, 0x02 };
            using var stream = new MemoryStream(data);

            // Act
            var methods = await Socks5ProtocolHandler.ReadGreetingAsync(stream, CancellationToken.None);

            // Assert
            Assert.Equal(2, methods.Length);
            Assert.Equal(Socks5Constants.AuthNoAuth, methods[0]);
            Assert.Equal(Socks5Constants.AuthUsernamePassword, methods[1]);
        }

        [Fact]
        public async Task ReadGreeting_ZeroMethods_ReturnsEmptyArray()
        {
            // Arrange: [NMETHODS=0]
            var data = new byte[] { 0x00 };
            using var stream = new MemoryStream(data);

            // Act
            var methods = await Socks5ProtocolHandler.ReadGreetingAsync(stream, CancellationToken.None);

            // Assert
            Assert.Empty(methods);
        }

        [Fact]
        public async Task WriteMethodSelection_NoAuth_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            await Socks5ProtocolHandler.WriteMethodSelectionAsync(
                stream, Socks5Constants.AuthNoAuth, CancellationToken.None);

            // Assert
            var result = stream.ToArray();
            Assert.Equal(2, result.Length);
            Assert.Equal(Socks5Constants.Version, result[0]);
            Assert.Equal(Socks5Constants.AuthNoAuth, result[1]);
        }

        [Fact]
        public async Task WriteMethodSelection_UsernamePassword_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            await Socks5ProtocolHandler.WriteMethodSelectionAsync(
                stream, Socks5Constants.AuthUsernamePassword, CancellationToken.None);

            // Assert
            var result = stream.ToArray();
            Assert.Equal(2, result.Length);
            Assert.Equal(Socks5Constants.Version, result[0]);
            Assert.Equal(Socks5Constants.AuthUsernamePassword, result[1]);
        }

        [Fact]
        public async Task ReadUsernamePassword_ValidCredentials_ReturnsCredentials()
        {
            // Arrange: [VER=0x01, ULEN=4, "user", PLEN=4, "pass"]
            var username = "user";
            var password = "pass";
            var usernameBytes = Encoding.UTF8.GetBytes(username);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            var data = new byte[2 + usernameBytes.Length + 1 + passwordBytes.Length];
            data[0] = Socks5Constants.AuthVersion;
            data[1] = (byte)usernameBytes.Length;
            Buffer.BlockCopy(usernameBytes, 0, data, 2, usernameBytes.Length);
            data[2 + usernameBytes.Length] = (byte)passwordBytes.Length;
            Buffer.BlockCopy(passwordBytes, 0, data, 3 + usernameBytes.Length, passwordBytes.Length);

            using var stream = new MemoryStream(data);

            // Act
            var (resultUser, resultPass) = await Socks5ProtocolHandler.ReadUsernamePasswordAsync(
                stream, CancellationToken.None);

            // Assert
            Assert.Equal(username, resultUser);
            Assert.Equal(password, resultPass);
        }

        [Fact]
        public async Task WriteAuthReply_Success_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            await Socks5ProtocolHandler.WriteAuthReplyAsync(stream, true, CancellationToken.None);

            // Assert
            var result = stream.ToArray();
            Assert.Equal(2, result.Length);
            Assert.Equal(Socks5Constants.AuthVersion, result[0]);
            Assert.Equal(0x00, result[1]);
        }

        [Fact]
        public async Task WriteAuthReply_Failure_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            await Socks5ProtocolHandler.WriteAuthReplyAsync(stream, false, CancellationToken.None);

            // Assert
            var result = stream.ToArray();
            Assert.Equal(2, result.Length);
            Assert.Equal(Socks5Constants.AuthVersion, result[0]);
            Assert.Equal(0x01, result[1]);
        }

        [Fact]
        public async Task ReadRequest_IPv4Address_ParsesCorrectly()
        {
            // Arrange: [VER=0x05, CMD=0x01, RSV=0x00, ATYP=0x01, ADDR=192.168.1.1, PORT=80]
            var data = new byte[]
            {
                0x05, 0x01, 0x00, 0x01,  // header
                192, 168, 1, 1,           // IPv4
                0x00, 0x50                // port 80
            };
            using var stream = new MemoryStream(data);

            // Act
            var request = await Socks5ProtocolHandler.ReadRequestAsync(stream, CancellationToken.None);

            // Assert
            Assert.Equal(Socks5Constants.CmdConnect, request.Command);
            Assert.Equal(Socks5Constants.AddrTypeIPv4, request.AddressType);
            Assert.Equal("192.168.1.1", request.DestinationAddress);
            Assert.Equal(80, request.DestinationPort);
        }

        [Fact]
        public async Task ReadRequest_DomainName_ParsesCorrectly()
        {
            // Arrange: [VER=0x05, CMD=0x01, RSV=0x00, ATYP=0x03, LEN=11, "example.com", PORT=443]
            var domain = "example.com";
            var domainBytes = Encoding.ASCII.GetBytes(domain);

            var data = new byte[4 + 1 + domainBytes.Length + 2];
            data[0] = 0x05;
            data[1] = 0x01;
            data[2] = 0x00;
            data[3] = 0x03;
            data[4] = (byte)domainBytes.Length;
            Buffer.BlockCopy(domainBytes, 0, data, 5, domainBytes.Length);
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(5 + domainBytes.Length), 443);

            using var stream = new MemoryStream(data);

            // Act
            var request = await Socks5ProtocolHandler.ReadRequestAsync(stream, CancellationToken.None);

            // Assert
            Assert.Equal(Socks5Constants.CmdConnect, request.Command);
            Assert.Equal(Socks5Constants.AddrTypeDomain, request.AddressType);
            Assert.Equal(domain, request.DestinationAddress);
            Assert.Equal(443, request.DestinationPort);
        }

        [Fact]
        public async Task ReadRequest_IPv6Address_ParsesCorrectly()
        {
            // Arrange: [VER=0x05, CMD=0x01, RSV=0x00, ATYP=0x04, ADDR=::1, PORT=8080]
            var ipv6 = IPAddress.Parse("::1");
            var ipv6Bytes = ipv6.GetAddressBytes();

            var data = new byte[4 + 16 + 2];
            data[0] = 0x05;
            data[1] = 0x01;
            data[2] = 0x00;
            data[3] = 0x04;
            Buffer.BlockCopy(ipv6Bytes, 0, data, 4, 16);
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(20), 8080);

            using var stream = new MemoryStream(data);

            // Act
            var request = await Socks5ProtocolHandler.ReadRequestAsync(stream, CancellationToken.None);

            // Assert
            Assert.Equal(Socks5Constants.CmdConnect, request.Command);
            Assert.Equal(Socks5Constants.AddrTypeIPv6, request.AddressType);
            Assert.Equal("::1", request.DestinationAddress);
            Assert.Equal(8080, request.DestinationPort);
        }

        [Fact]
        public async Task WriteReply_Success_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();
            var bindAddress = new byte[] { 127, 0, 0, 1 };

            // Act
            await Socks5ProtocolHandler.WriteReplyAsync(
                stream,
                Socks5Constants.RepSucceeded,
                Socks5Constants.AddrTypeIPv4,
                bindAddress,
                1080,
                CancellationToken.None);

            // Assert
            var result = stream.ToArray();
            Assert.Equal(10, result.Length); // 4 header + 4 IPv4 + 2 port
            Assert.Equal(Socks5Constants.Version, result[0]);
            Assert.Equal(Socks5Constants.RepSucceeded, result[1]);
            Assert.Equal(0x00, result[2]); // RSV
            Assert.Equal(Socks5Constants.AddrTypeIPv4, result[3]);
            Assert.Equal(127, result[4]);
            Assert.Equal(0, result[5]);
            Assert.Equal(0, result[6]);
            Assert.Equal(1, result[7]);
            Assert.Equal(0x04, result[8]); // port 1080 high byte
            Assert.Equal(0x38, result[9]); // port 1080 low byte
        }

        [Fact]
        public async Task WriteErrorReply_CommandNotSupported_WritesCorrectBytes()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            await Socks5ProtocolHandler.WriteErrorReplyAsync(
                stream,
                Socks5Constants.RepCommandNotSupported,
                CancellationToken.None);

            // Assert
            var result = stream.ToArray();
            Assert.Equal(10, result.Length);
            Assert.Equal(Socks5Constants.Version, result[0]);
            Assert.Equal(Socks5Constants.RepCommandNotSupported, result[1]);
        }

        [Fact]
        public async Task ReadRequest_UnsupportedAddressType_ThrowsException()
        {
            // Arrange: Invalid address type 0x05
            var data = new byte[] { 0x05, 0x01, 0x00, 0x05 };
            using var stream = new MemoryStream(data);

            // Act & Assert
            await Assert.ThrowsAsync<Socks5ProtocolException>(
                () => Socks5ProtocolHandler.ReadRequestAsync(stream, CancellationToken.None).AsTask());
        }

        [Fact]
        public async Task ReadUsernamePassword_InvalidVersion_ThrowsException()
        {
            // Arrange: Invalid version 0x02
            var data = new byte[] { 0x02, 0x04, 0x75, 0x73, 0x65, 0x72, 0x04, 0x70, 0x61, 0x73, 0x73 };
            using var stream = new MemoryStream(data);

            // Act & Assert
            await Assert.ThrowsAsync<Socks5ProtocolException>(
                () => Socks5ProtocolHandler.ReadUsernamePasswordAsync(stream, CancellationToken.None).AsTask());
        }
    }
}
