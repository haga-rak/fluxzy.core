// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Core
{
    /// <summary>
    ///  A SOCK5 listener that can be used to create a connection to a remote server.
    ///  Does not support Authentication and UDP.
    /// </summary>
    internal class FromSock5SourceProvider : ExchangeSourceProvider
    {
        private readonly Sock5Credential? _sock5Credential;
        private readonly Sock5AuthenticationMethod[] _supportedMethods;

        public FromSock5SourceProvider(IIdProvider idProvider, Sock5Credential?  sock5Credential)
            : base(idProvider)
        {
            _sock5Credential = sock5Credential;
            _supportedMethods = _sock5Credential != null
                ? new[] {Sock5AuthenticationMethod.UsernamePassword}
                : new[] {Sock5AuthenticationMethod.NoAuthentication, Sock5AuthenticationMethod.UsernamePassword };
        }

        public override async ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream, RsBuffer buffer, IExchangeContextBuilder contextBuilder, IPEndPoint requestedEndpoint,
            CancellationToken token)
        {
            // Negotiate the authentication method 

            var availableMethods = await Sock5Helper.ReadClientHandshake(stream, token);

            var negotiatedMethods 
                = _supportedMethods.Intersect(availableMethods).OrderBy(m => m).ToList();

            if (!negotiatedMethods.Any()) {
                await Sock5Helper.WriteServerHandshakeResponse(stream, Sock5AuthenticationMethod.NoAcceptableMethods, token);
                return null;
            }

            var negotiatedMethod = negotiatedMethods.First();

            await Sock5Helper.WriteServerHandshakeResponse(stream, negotiatedMethod, token);

            if (negotiatedMethod == Sock5AuthenticationMethod.UsernamePassword) {
                if (_sock5Credential == null)
                    throw new Sock5Exception("No credential provided for Username/Password authentication");

                var credential = await Sock5Helper.ReadClientCredential(stream, token);

                if (credential.Username != _sock5Credential.Username ||
                    credential.Password != _sock5Credential.Password) {
                    await Sock5Helper.WriteServerCredentialResponse(stream, false, token);
                    throw new UnauthorizedAccessException("Invalid SOCKS5 Username/Password");
                }

                await Sock5Helper.WriteServerCredentialResponse(stream, true, token);
            }

            // Authenticate if needed 

            // Read the destination

            var destination = await Sock5Helper.ReadDestination(stream, buffer, token);

            var client = new TcpClient();

            try
            {
                await client.ConnectAsync(destination.Address, destination.Port, token);

                await Sock5Helper.SendDestinationResponse(stream, Sock5DestinationReply.Succeeded,
                    destination, token);
            }
            catch (Exception ex) {
                await Sock5Helper.SendDestinationResponse(stream, Sock5DestinationReply.HostUnreachable,
                    destination, token);
            }

            var exchangeContext = await contextBuilder.Create(new Authority(requestedEndpoint.Address.ToString(),
                requestedEndpoint.Port, true), true);

            var authority = new Authority(destination.Address, destination.Port, )

            var exchange = Exchange.CreateUntrackedExchange(IdProvider, exchangeContext,
                new Authority(requestedEndpoint.Address.ToString(), requestedEndpoint.Port, true),
                               StreamUtils.EmptyStream, StreamUtils.EmptyStream, StreamUtils.EmptyStream, StreamUtils.EmptyStream,
                               false, "HTTP/1.1", ITimingProvider.Default.Instant());


            return new ExchangeSourceInitResult()
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

    internal enum Sock5AuthenticationMethod
    {
        NoAuthentication = 0x00,
        UsernamePassword = 0x02,
        NoAcceptableMethods = 0xFF
    }


    internal class Sock5Credential
    {
        public Sock5Credential(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username { get; }

        public string Password { get; }
    }
}
