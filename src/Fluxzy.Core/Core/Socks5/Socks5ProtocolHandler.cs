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
        /// <summary>
        /// Reads the SOCKS5 greeting from client.
        /// Format: [VER, NMETHODS, METHODS...]
        /// The first byte (VER=0x05) should already be consumed.
        /// </summary>
        public static async ValueTask<byte[]> ReadGreetingAsync(
            Stream stream,
            CancellationToken token)
        {
            var buffer = new byte[1];

            // Read NMETHODS
            if (await stream.ReadAsync(buffer, token).ConfigureAwait(false) != 1)
                throw new Socks5ProtocolException("Failed to read NMETHODS");

            var nMethods = buffer[0];
            if (nMethods == 0)
                return Array.Empty<byte>();

            // Read METHODS
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
        /// Writes the method selection response to client.
        /// Format: [VER, METHOD]
        /// </summary>
        public static async ValueTask WriteMethodSelectionAsync(
            Stream stream,
            byte method,
            CancellationToken token)
        {
            var response = new byte[] { Socks5Constants.Version, method };
            await stream.WriteAsync(response, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads username/password authentication.
        /// Format: [VER, ULEN, UNAME, PLEN, PASSWD]
        /// </summary>
        public static async ValueTask<(string username, string password)> ReadUsernamePasswordAsync(
            Stream stream,
            CancellationToken token)
        {
            var header = new byte[2];

            // Read VER and ULEN
            if (await ReadExactAsync(stream, header, token).ConfigureAwait(false) != 2)
                throw new Socks5ProtocolException("Failed to read auth header");

            if (header[0] != Socks5Constants.AuthVersion)
                throw new Socks5ProtocolException($"Invalid auth version: {header[0]}");

            var usernameLen = header[1];
            var usernameBytes = new byte[usernameLen];

            if (await ReadExactAsync(stream, usernameBytes, token).ConfigureAwait(false) != usernameLen)
                throw new Socks5ProtocolException("Failed to read username");

            // Read PLEN
            var plenBuffer = new byte[1];
            if (await ReadExactAsync(stream, plenBuffer, token).ConfigureAwait(false) != 1)
                throw new Socks5ProtocolException("Failed to read password length");

            var passwordLen = plenBuffer[0];
            var passwordBytes = new byte[passwordLen];

            if (await ReadExactAsync(stream, passwordBytes, token).ConfigureAwait(false) != passwordLen)
                throw new Socks5ProtocolException("Failed to read password");

            return (
                Encoding.UTF8.GetString(usernameBytes),
                Encoding.UTF8.GetString(passwordBytes)
            );
        }

        /// <summary>
        /// Writes authentication reply.
        /// Format: [VER, STATUS] where STATUS=0x00 for success
        /// </summary>
        public static async ValueTask WriteAuthReplyAsync(
            Stream stream,
            bool success,
            CancellationToken token)
        {
            var response = new byte[] { Socks5Constants.AuthVersion, success ? (byte)0x00 : (byte)0x01 };
            await stream.WriteAsync(response, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads the SOCKS5 connect request.
        /// Format: [VER, CMD, RSV, ATYP, DST.ADDR, DST.PORT]
        /// </summary>
        public static async ValueTask<Socks5Request> ReadRequestAsync(
            Stream stream,
            CancellationToken token)
        {
            var header = new byte[4];

            if (await ReadExactAsync(stream, header, token).ConfigureAwait(false) != 4)
                throw new Socks5ProtocolException("Failed to read request header");

            if (header[0] != Socks5Constants.Version)
                throw new Socks5ProtocolException($"Invalid SOCKS version in request: {header[0]}");

            var command = header[1];
            var addressType = header[3];

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
                    var domainLenBuffer = new byte[1];
                    if (await ReadExactAsync(stream, domainLenBuffer, token).ConfigureAwait(false) != 1)
                        throw new Socks5ProtocolException("Failed to read domain length");

                    var domainLen = domainLenBuffer[0];
                    var domainBytes = new byte[domainLen];

                    if (await ReadExactAsync(stream, domainBytes, token).ConfigureAwait(false) != domainLen)
                        throw new Socks5ProtocolException("Failed to read domain name");

                    address = Encoding.ASCII.GetString(domainBytes);
                    rawAddress = new byte[1 + domainLen];
                    rawAddress[0] = domainLen;
                    Buffer.BlockCopy(domainBytes, 0, rawAddress, 1, domainLen);
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

            // Read port (2 bytes, big-endian)
            var portBuffer = new byte[Socks5Constants.PortLength];
            if (await ReadExactAsync(stream, portBuffer, token).ConfigureAwait(false) != Socks5Constants.PortLength)
                throw new Socks5ProtocolException("Failed to read port");

            var port = BinaryPrimitives.ReadUInt16BigEndian(portBuffer);

            return new Socks5Request(command, addressType, address, port, rawAddress);
        }

        /// <summary>
        /// Writes the SOCKS5 reply.
        /// Format: [VER, REP, RSV, ATYP, BND.ADDR, BND.PORT]
        /// </summary>
        public static async ValueTask WriteReplyAsync(
            Stream stream,
            byte replyCode,
            byte addressType,
            byte[] bindAddress,
            int bindPort,
            CancellationToken token)
        {
            var portBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(portBytes, (ushort)bindPort);

            var responseLength = 4 + bindAddress.Length + 2;
            var response = new byte[responseLength];

            response[0] = Socks5Constants.Version;
            response[1] = replyCode;
            response[2] = Socks5Constants.Reserved;
            response[3] = addressType;

            Buffer.BlockCopy(bindAddress, 0, response, 4, bindAddress.Length);
            Buffer.BlockCopy(portBytes, 0, response, 4 + bindAddress.Length, 2);

            await stream.WriteAsync(response, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes a simple error reply with IPv4 zero address.
        /// </summary>
        public static ValueTask WriteErrorReplyAsync(
            Stream stream,
            byte errorCode,
            CancellationToken token)
        {
            return WriteReplyAsync(
                stream,
                errorCode,
                Socks5Constants.AddrTypeIPv4,
                new byte[Socks5Constants.IPv4AddressLength],
                0,
                token);
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
