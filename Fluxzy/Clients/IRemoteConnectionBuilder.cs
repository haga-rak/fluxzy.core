﻿// Copyright © 2021 Haga Rakotoharivelo

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc;

namespace Fluxzy.Clients
{
    internal enum RemoteConnectionResultType : byte
    {
        Unknown = 0,
        Http11,
        Http2
    }

    internal readonly struct RemoteConnectionResult
    {
        public RemoteConnectionResult(RemoteConnectionResultType type, Connection connection)
        {
            Type = type;
            Connection = connection;
        }

        public RemoteConnectionResultType Type { get; }
        
        public Connection Connection { get;  }
    }

    internal class RemoteConnectionBuilder
    {
        private readonly ITimingProvider _timeProvider;
        private readonly IDnsSolver _dnsSolver;

        public RemoteConnectionBuilder(ITimingProvider timeProvider, IDnsSolver dnsSolver)
        {
            _timeProvider = timeProvider;
            _dnsSolver = dnsSolver;
        }

        public async ValueTask<RemoteConnectionResult> OpenConnectionToRemote(
            Authority authority, 
            ExchangeContext context,
            List<SslApplicationProtocol> httpProtocols,
            ProxyRuntimeSetting setting, 
            CancellationToken token)
        {
            var tcpClient = new TcpClient();
            
            var connection = new Connection(authority)
            {
                TcpConnectionOpening = _timeProvider.Instant()
            };
            

           // tcpClient.LingerState.

            connection.DnsSolveStart = _timeProvider.Instant();

            var ipAddress = context.RemoteHostIp ?? 
                            await _dnsSolver.SolveDns(authority.HostName);

            connection.RemoteAddress = ipAddress;

            connection.DnsSolveEnd = _timeProvider.Instant();

            await tcpClient.ConnectAsync(ipAddress, context.RemoteHostPort ?? 
                authority.Port).ConfigureAwait(false);

            var localEndpoint = (IPEndPoint) tcpClient.Client.LocalEndPoint;

            connection.TcpConnectionOpened = _timeProvider.Instant();
            connection.LocalPort = localEndpoint.Port;
            connection.LocalAddress = localEndpoint.Address.ToString();
            
            var newlyOpenedStream = tcpClient.GetStream();
            
            if (!authority.Secure || context.BlindMode)
            {
                connection.ReadStream = connection.WriteStream = newlyOpenedStream;
                return new RemoteConnectionResult(RemoteConnectionResultType.Unknown,  connection);
            }

            connection.SslNegotiationStart = _timeProvider.Instant();

            var sslStream = new SslStream(newlyOpenedStream, false, setting.CertificateValidationCallback);

            Stream resultStream = sslStream; 

            SslClientAuthenticationOptions authenticationOptions = new SslClientAuthenticationOptions()
            {
                ClientCertificates = context.ClientCertificates, 
                TargetHost = authority.HostName , 
                EnabledSslProtocols = setting.ProxyTlsProtocols,
                ApplicationProtocols = httpProtocols
            };
            
            await sslStream.AuthenticateAsClientAsync(authenticationOptions, token).ConfigureAwait(false);

            connection.SslInfo = new SslInfo(sslStream); 

            connection.SslNegotiationEnd = _timeProvider.Instant();

            if (DebugContext.EnableNetworkFileDump)
            {
                resultStream = new DebugFileStream($"raw/{connection.Id:000000}_remotehost_",
                    resultStream); 
            }

            var protoType =  sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2
                ? RemoteConnectionResultType.Http2
                : RemoteConnectionResultType.Http11;

            connection.ReadStream = connection.WriteStream = resultStream;
            

            return new RemoteConnectionResult(protoType, connection);
        }
    }
}