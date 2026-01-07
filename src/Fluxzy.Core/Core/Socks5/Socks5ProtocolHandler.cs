// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Core.Socks5
{
    internal static class Socks5ProtocolHandler
    {
        private static readonly byte[] SuccessResponse = { Socks5Constants.AuthVersion, 0x00 };
        private static readonly byte[] FailureResponse = { Socks5Constants.AuthVersion, 0x01 };
        private static readonly byte[] ZeroIPv4Address = new byte[Socks5Constants.IPv4AddressLength];

        // Pre-allocated method selection responses for common cases
        private static readonly byte[] MethodSelectionNoAuth = { Socks5Constants.Version, Socks5Constants.AuthNoAuth };
        private static readonly byte[] MethodSelectionUsernamePassword = { Socks5Constants.Version, Socks5Constants.AuthUsernamePassword };
        private static readonly byte[] MethodSelectionNoAcceptable = { Socks5Constants.Version, Socks5Constants.AuthNoAcceptable };

        /// <summary>
        /// Reads the SOCKS5 greeting from client using the provided work buffer.
        /// Format: [VER, NMETHODS, METHODS...]
        /// The first byte (VER=0x05) should already be consumed.
        /// </summary>
        public static async ValueTask<byte[]> ReadGreetingAsync(
            Stream stream,
            Memory<byte> workBuffer,
            CancellationToken token)
        {
            // Read NMETHODS using work buffer
            if (await stream.ReadAsync(workBuffer.Slice(0, 1), token).ConfigureAwait(false) != 1)
                throw new Socks5ProtocolException("Failed to read NMETHODS");

            var nMethods = workBuffer.Span[0];
            if (nMethods == 0)
                return Array.Empty<byte>();

            // Read METHODS - must allocate as this is returned
            var methods = new byte[nMethods];
            var totalRead = 0;

            while (totalRead < nMethods)
            {
                var read = await stream.ReadAsync(
                    methods.AsMemory(totalRead, nMethods - totalRead), token).ConfigureAwait(false);

                if (read == 0)
                    throw new Socks5ProtocolException("Connection closed while reading auth methods");

                totalRead += read;
            }

            return methods;
        }

        /// <summary>
        /// Writes the method selection response to client using pre-allocated buffers.
        /// Format: [VER, METHOD]
        /// </summary>
        public static async ValueTask WriteMethodSelectionAsync(
            Stream stream,
            byte method,
            Memory<byte> workBuffer,
            CancellationToken token)
        {
            // Use pre-allocated static arrays for common methods
            ReadOnlyMemory<byte> response = method switch
            {
                Socks5Constants.AuthNoAuth => MethodSelectionNoAuth,
                Socks5Constants.AuthUsernamePassword => MethodSelectionUsernamePassword,
                Socks5Constants.AuthNoAcceptable => MethodSelectionNoAcceptable,
                _ => BuildMethodSelection(method, workBuffer)
            };

            await stream.WriteAsync(response, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
        }

        private static Memory<byte> BuildMethodSelection(byte method, Memory<byte> workBuffer)
        {
            var span = workBuffer.Span;
            span[0] = Socks5Constants.Version;
            span[1] = method;
            return workBuffer.Slice(0, 2);
        }

        /// <summary>
        /// Reads username/password authentication using the provided work buffer.
        /// Format: [VER, ULEN, UNAME, PLEN, PASSWD]
        /// </summary>
        public static async ValueTask<(string username, string password)> ReadUsernamePasswordAsync(
            Stream stream,
            Memory<byte> workBuffer,
            CancellationToken token)
        {
            // Read VER and ULEN using work buffer
            if (await ReadExactAsync(stream, workBuffer.Slice(0, 2), token).ConfigureAwait(false) != 2)
                throw new Socks5ProtocolException("Failed to read auth header");

            // Access span inline to avoid storing ref struct across await
            if (workBuffer.Span[0] != Socks5Constants.AuthVersion)
                throw new Socks5ProtocolException($"Invalid auth version: {workBuffer.Span[0]}");

            var usernameLen = workBuffer.Span[1];

            // Read username into work buffer
            if (await ReadExactAsync(stream, workBuffer.Slice(0, usernameLen), token).ConfigureAwait(false) != usernameLen)
                throw new Socks5ProtocolException("Failed to read username");

            var username = Encoding.ASCII.GetString(workBuffer.Slice(0, usernameLen).Span.TrimEnd((byte) 0));

            // Read PLEN
            if (await ReadExactAsync(stream, workBuffer.Slice(0, 1), token).ConfigureAwait(false) != 1)
                throw new Socks5ProtocolException("Failed to read password length");

            var passwordLen = workBuffer.Span[0];

            // Read password into work buffer
            if (await ReadExactAsync(stream, workBuffer.Slice(0, passwordLen), token).ConfigureAwait(false) != passwordLen)
                throw new Socks5ProtocolException("Failed to read password");

            var password = Encoding.ASCII.GetString(workBuffer.Slice(0, passwordLen).Span.TrimEnd((byte)0));

            return (username, password);
        }

        /// <summary>
        /// Writes authentication reply.
        /// Format: [VER, STATUS] where STATUS=0x00 for success
        /// </summary>
        public static ValueTask WriteAuthReplyAsync(
            Stream stream,
            bool success,
            CancellationToken token)
        {
            // Pre-allocated static arrays — zero allocation
            return stream.WriteAsync(success ? SuccessResponse : FailureResponse, token);
        }

        /// <summary>
        /// Reads the SOCKS5 connect request using the provided work buffer.
        /// Format: [VER, CMD, RSV, ATYP, DST.ADDR, DST.PORT]
        /// </summary>
        public static async ValueTask<Socks5Request> ReadRequestAsync(
            Stream stream,
            Memory<byte> workBuffer,
            CancellationToken token)
        {
            // Read header using work buffer
            if (await ReadExactAsync(stream, workBuffer.Slice(0, 4), token).ConfigureAwait(false) != 4)
                throw new Socks5ProtocolException("Failed to read request header");

            // Access span inline to avoid storing ref struct across await
            if (workBuffer.Span[0] != Socks5Constants.Version)
                throw new Socks5ProtocolException($"Invalid SOCKS version in request: {workBuffer.Span[0]}");

            var command = workBuffer.Span[1];
            var addressType = workBuffer.Span[3];

            string address;
            byte[] rawAddress;

            switch (addressType)
            {
                case Socks5Constants.AddrTypeIPv4:
                    rawAddress = new byte[Socks5Constants.IPv4AddressLength];
                    if (await ReadExactAsync(stream, rawAddress, token).ConfigureAwait(false) != Socks5Constants.IPv4AddressLength)
                        throw new Socks5ProtocolException("Failed to read IPv4 address");
                    address = new IPAddress(rawAddress).ToString();
                    break;

                case Socks5Constants.AddrTypeDomain:
                    // Read domain length using work buffer
                    if (await ReadExactAsync(stream, workBuffer.Slice(0, 1), token).ConfigureAwait(false) != 1)
                        throw new Socks5ProtocolException("Failed to read domain length");

                    var domainLen = workBuffer.Span[0];

                    // Read domain into work buffer
                    if (await ReadExactAsync(stream, workBuffer.Slice(0, domainLen), token).ConfigureAwait(false) != domainLen)
                        throw new Socks5ProtocolException("Failed to read domain name");

                    address = Encoding.ASCII.GetString(workBuffer.Slice(0, domainLen).Span.TrimEnd((byte)0));

                    rawAddress = new byte[1 + domainLen];
                    rawAddress[0] = domainLen;
                    workBuffer.Slice(0, domainLen).CopyTo(rawAddress.AsMemory(1));
                    break;

                case Socks5Constants.AddrTypeIPv6:
                    rawAddress = new byte[Socks5Constants.IPv6AddressLength];
                    if (await ReadExactAsync(stream, rawAddress, token).ConfigureAwait(false) != Socks5Constants.IPv6AddressLength)
                        throw new Socks5ProtocolException("Failed to read IPv6 address");
                    address = new IPAddress(rawAddress).ToString();
                    break;

                default:
                    throw new Socks5ProtocolException($"Unsupported address type: {addressType}");
            }

            // Read port using work buffer
            if (await ReadExactAsync(stream, workBuffer.Slice(0, Socks5Constants.PortLength), token).ConfigureAwait(false) != Socks5Constants.PortLength)
                throw new Socks5ProtocolException("Failed to read port");

            // Read port bytes directly to avoid Span in async method
            var port = (ushort)((workBuffer.Span[0] << 8) | workBuffer.Span[1]);

            return new Socks5Request(command, addressType, address, port, rawAddress);
        }

        /// <summary>
        /// Writes the SOCKS5 reply using the provided work buffer.
        /// Format: [VER, REP, RSV, ATYP, BND.ADDR, BND.PORT]
        /// </summary>
        public static async ValueTask WriteReplyAsync(
            Stream stream,
            byte replyCode,
            byte addressType,
            byte[] bindAddress,
            int bindPort,
            Memory<byte> workBuffer,
            CancellationToken token)
        {
            var responseLength = BuildReplyBuffer(replyCode, addressType, bindAddress, bindPort, workBuffer);
            await stream.WriteAsync(workBuffer.Slice(0, responseLength), token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
        }

        private static int BuildReplyBuffer(
            byte replyCode,
            byte addressType,
            byte[] bindAddress,
            int bindPort,
            Memory<byte> workBuffer)
        {
            var responseLength = 4 + bindAddress.Length + 2;
            var span = workBuffer.Span;

            span[0] = Socks5Constants.Version;
            span[1] = replyCode;
            span[2] = Socks5Constants.Reserved;
            span[3] = addressType;

            bindAddress.CopyTo(span.Slice(4));
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(4 + bindAddress.Length), (ushort)bindPort);

            return responseLength;
        }

        /// <summary>
        /// Writes a simple error reply with IPv4 zero address using pre-allocated buffer.
        /// </summary>
        public static ValueTask WriteErrorReplyAsync(
            Stream stream,
            byte errorCode,
            Memory<byte> workBuffer,
            CancellationToken token)
        {
            return WriteReplyAsync(
                stream,
                errorCode,
                Socks5Constants.AddrTypeIPv4,
                ZeroIPv4Address,
                0,
                workBuffer,
                token);
        }

        private static async ValueTask<int> ReadExactAsync(
            Stream stream,
            Memory<byte> buffer,
            CancellationToken token)
        {
            var totalRead = 0;
            var length = buffer.Length;

            while (totalRead < length)
            {
                var read = await stream.ReadAsync(
                    buffer.Slice(totalRead, length - totalRead), token).ConfigureAwait(false);

                if (read == 0)
                    return totalRead;

                totalRead += read;
            }

            return totalRead;
        }

        private static async ValueTask<int> ReadExactAsync(
            Stream stream,
            byte[] buffer,
            CancellationToken token)
        {
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var read = await stream.ReadAsync(
                    buffer.AsMemory(totalRead, buffer.Length - totalRead), token).ConfigureAwait(false);

                if (read == 0)
                    return totalRead;

                totalRead += read;
            }

            return totalRead;
        }
    }
}
