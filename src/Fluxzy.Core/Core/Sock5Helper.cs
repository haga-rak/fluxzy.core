// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    internal static class Sock5Helper
    {
        public static async ValueTask WriteAccept(Stream stream, Sock5AuthenticationMethod method)
        {
            var buffer = new byte[] { 0x05, (byte) method };
            await stream.WriteAsync(buffer);
        }

        public static async ValueTask<Sock5AuthenticationMethod[]> ReadClientHandshake(Stream stream, CancellationToken token)
        {
            var buffer = new byte[2];
            var readResult = await stream.ReadExactAsync(buffer, token);

            if (!readResult)
                throw new Sock5Exception("EOF while trying to read SOCK5 client handshake");

            var methodCount = buffer[1];

            var methodsBuffer = new byte[methodCount];

            readResult = await stream.ReadExactAsync(methodsBuffer, token);

            if (!readResult)
                throw new Sock5Exception("EOF while trying to read SOCK5 client handshake");

            return methodsBuffer.Select(b => (Sock5AuthenticationMethod)b).ToArray();
        }
        
        public static async ValueTask WriteServerHandshakeResponse(
            Stream stream, Sock5AuthenticationMethod method, CancellationToken token)
        {
            var buffer = new byte[] { 0x05, (byte) method };
            await stream.WriteAsync(buffer, token);
        }

        public static async ValueTask<Sock5Credential> ReadClientCredential(Stream stream, CancellationToken token)
        {
            var buffer = new byte[2];
            var readResult = await stream.ReadExactAsync(buffer, token);

            if (!readResult)
                throw new Sock5Exception("EOF while trying to read SOCK5 client credential");

            if (buffer[0] != 0x01)
                throw new Sock5Exception("Invalid SOCK5 client credential version");

            var usernameLength = buffer[1];

            var usernameBuffer = new byte[usernameLength];

            readResult = await stream.ReadExactAsync(usernameBuffer, token);

            if (!readResult)
                throw new Sock5Exception("EOF while trying to read SOCK5 client credential");

            var passwordLength = buffer[1];

            var passwordBuffer = new byte[passwordLength];

            readResult = await stream.ReadExactAsync(passwordBuffer, token);

            if (!readResult)
                throw new Sock5Exception("EOF while trying to read SOCK5 client credential");

            return new Sock5Credential(Encoding.UTF8.GetString(usernameBuffer), Encoding.UTF8.GetString(passwordBuffer));

        }

        public static async ValueTask WriteServerCredentialResponse(
            Stream stream, bool success, CancellationToken token)
        {
            var buffer = new byte[] { 0x01, (byte) (success ? 0x00 : 0x01) };
            await stream.WriteAsync(buffer, token);
        }

        public static async ValueTask<Sock5Destination> ReadDestination(Stream stream, RsBuffer buffer, CancellationToken token)
        {
            var readResult = await stream.ReadExactAsync(buffer.Memory.Slice(0, 4), token);

            if (!readResult)
                throw new Sock5Exception("EOF while trying to read SOCK5 header");

            // validate that the first 3 bytes is a valid SOCK5 header

            if (buffer.Memory.Span[0] != 0x05)
                throw new Sock5Exception("Invalid SOCK5 header");

            if (buffer.Memory.Span[1] != 0x01)
                throw new Sock5Exception("Invalid SOCK5 header. Must be connect");

            if (buffer.Memory.Span[2] != 0x00)
                throw new Sock5Exception("Invalid SOCK5 header. Reserved byte must be 0x00");

            var destinationType = (Sock5DestinationType)buffer.Memory.Span[3];

            string destinationAddress;

            switch (destinationType)
            {
                case Sock5DestinationType.IPv4:

                    if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, 4), token))
                        throw new Sock5Exception("EOF while trying to read SOCK5 destination IPv4 address");

                    destinationAddress = new IPAddress(buffer.Memory.Slice(0, 4).ToArray()).ToString();

                    break;

                case Sock5DestinationType.IPv6:
                    if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, 16), token))
                        throw new Sock5Exception("EOF while trying to read SOCK5 destination 6 address");

                    destinationAddress = new IPAddress(buffer.Memory.Slice(0, 16).ToArray()).ToString();

                    break;

                case Sock5DestinationType.DomainName:

                    if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, 1), token))
                        throw new Sock5Exception("EOF while trying to read SOCK5 destination domain name length");

                    var stringLength = buffer.Memory.Span[0];

                    if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, stringLength), token))
                        throw new Sock5Exception("EOF while trying to read SOCK5 destination domain name");

                    destinationAddress = Encoding.UTF8.GetString(buffer.Memory.Slice(0, stringLength).ToArray());

                    break;

                default:
                    throw new Sock5Exception("Invalid SOCK5 destination type");
            }

            if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, 2), token))
                throw new Sock5Exception("EOF while trying to read SOCK5 destination port");

            var destinationPort = BinaryPrimitives.ReadUInt16BigEndian(buffer.Memory.Span);

            Sock5Destination destination = new(destinationType, destinationAddress, destinationPort);

            return destination;
        }

        public static async Task SendDestinationResponse(Stream stream, Sock5DestinationReply reply,
            Sock5Destination destination, CancellationToken token)
        {
            var buffer = new byte[10];

            buffer[0] = 0x05;
            buffer[1] = (byte) reply;
            buffer[2] = 0x00;

            switch (destination.Type) {
                case Sock5DestinationType.IPv4:
                    buffer[3] = 0x01;
                    IPAddress.Parse(destination.Address).TryWriteBytes(buffer.AsSpan(4), out _);
                    break;

                case Sock5DestinationType.IPv6:
                    buffer[3] = 0x04;
                    IPAddress.Parse(destination.Address).TryWriteBytes(buffer.AsSpan(4), out _);
                    break;

                case Sock5DestinationType.DomainName:
                    buffer[3] = 0x03;
                    buffer[4] = (byte) destination.Address.Length;
                    Encoding.UTF8.GetBytes(destination.Address).CopyTo(buffer, 5);
                    break;

                default:
                    throw new Sock5Exception("Invalid SOCK5 destination type");
            }

            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(8), (ushort) destination.Port);

            await stream.WriteAsync(buffer, token);
        }

    }

    public class Sock5Exception : Exception
    {
        public Sock5Exception(string message)
            : base(message)
        {
        }
    }

    public enum Sock5DestinationReply
    {
        Succeeded = 0x00,
        GeneralFailure = 0x01,
        ConnectionNotAllowed = 0x02,
        NetworkUnreachable = 0x03,
        HostUnreachable = 0x04,
        ConnectionRefused = 0x05,
        TTLExpired = 0x06,
        CommandNotSupported = 0x07,
        AddressTypeNotSupported = 0x08
    }
}
