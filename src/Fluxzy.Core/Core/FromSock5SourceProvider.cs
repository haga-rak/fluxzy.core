// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Misc;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    /// <summary>
    ///  A SOCK5 listener that can be used to create a connection to a remote server.
    ///  Does not support Authentication and UDP.
    /// </summary>
    internal class FromSock5SourceProvider : ExchangeSourceProvider
    {
        public FromSock5SourceProvider(IIdProvider idProvider)
            : base(idProvider)
        {

        }

        public override async ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream, RsBuffer buffer, IExchangeContextBuilder contextBuilder, IPEndPoint requestedEndpoint,
            CancellationToken token)
        {
            var destination = await Sock5Helper.ReadDestination(stream, buffer, token);



        }

    }

    internal enum Sock5DestinationType
    {
        IPv4 = 0x01,
        DomainName = 0x03,
        IPv6 = 0x04
    }

    internal struct Sock5Destination
    {
        public Sock5DestinationType Type { get; }

        public string Address { get; }

        public int Port { get; }

        public Sock5Destination(Sock5DestinationType type, string address, int port)
        {
            Type = type;
            Address = address;
            Port = port;
        }
    }

    internal static class Sock5Helper
    {
        public static async Task<Sock5Destination> ReadDestination(Stream stream, RsBuffer buffer, CancellationToken token)
        {
            var readResult = await stream.ReadExactAsync(buffer.Memory.Slice(0, 4), token);

            if (!readResult)
                throw new InvalidOperationException("EOF while trying to read SOCK5 header");

            // validate that the first 3 bytes is a valid SOCK5 header

            if (buffer.Memory.Span[0] != 0x05)
                throw new InvalidOperationException("Invalid SOCK5 header");

            if (buffer.Memory.Span[1] != 0x01)
                throw new InvalidOperationException("Invalid SOCK5 header. Must be connect");

            if (buffer.Memory.Span[2] != 0x00)
                throw new InvalidOperationException("Invalid SOCK5 header. Reserved byte must be 0x00");

            var destinationType = (Sock5DestinationType)buffer.Memory.Span[3];

            string destinationAddress;

            switch (destinationType)
            {
                case Sock5DestinationType.IPv4:

                    if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, 4), token))
                        throw new InvalidOperationException("EOF while trying to read SOCK5 destination IPv4 address");

                    destinationAddress = new IPAddress(buffer.Memory.Slice(0, 4).ToArray()).ToString();

                    break;

                case Sock5DestinationType.IPv6:
                    if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, 16), token))
                        throw new InvalidOperationException("EOF while trying to read SOCK5 destination 6 address");

                    destinationAddress = new IPAddress(buffer.Memory.Slice(0, 16).ToArray()).ToString();

                    break;

                case Sock5DestinationType.DomainName:

                    if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, 1), token))
                        throw new InvalidOperationException("EOF while trying to read SOCK5 destination domain name length");

                    var stringLength = buffer.Memory.Span[0];

                    if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, stringLength), token))
                        throw new InvalidOperationException("EOF while trying to read SOCK5 destination domain name");

                    destinationAddress = Encoding.UTF8.GetString(buffer.Memory.Slice(0, stringLength).ToArray());

                    break;

                default:
                    throw new InvalidOperationException("Invalid SOCK5 destination type");
            }

            if (!await stream.ReadExactAsync(buffer.Memory.Slice(0, 2), token))
                throw new InvalidOperationException("EOF while trying to read SOCK5 destination port");

            var destinationPort = BinaryPrimitives.ReadUInt16BigEndian(buffer.Memory.Span);

            Sock5Destination destination = new(destinationType, destinationAddress, destinationPort);

            return destination; 
        }
    }
}
