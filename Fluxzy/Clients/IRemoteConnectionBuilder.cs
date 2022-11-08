// Copyright © 2021 Haga Rakotoharivelo

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

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
            
            var connection = new Connection(authority, setting.IdProvider)
            {
                TcpConnectionOpening = _timeProvider.Instant(),
                // tcpClient.LingerState.
                DnsSolveStart = _timeProvider.Instant()
            };

            var ipAddress = context.RemoteHostIp ?? 
                            await _dnsSolver.SolveDns(authority.HostName);

            connection.RemoteAddress = ipAddress;
            connection.DnsSolveEnd = _timeProvider.Instant();

            var tcpClient = setting.TcpConnectionProvider
                .Create(setting.ArchiveWriter != null ?
                    setting.ArchiveWriter?.GetDumpfilePath(connection.Id)!
                    : string.Empty);

            var localEndpoint = await tcpClient.ConnectAsync(ipAddress, context.RemoteHostPort ?? 
                                                                        authority.Port).ConfigureAwait(false);

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

            byte[]? remoteCertificate = null;

            var sslStream =
                context.SkipRemoteCertificateValidation
                    ? new SslStream(newlyOpenedStream, false, (_, _, _, errors) => true)
                    : new SslStream(newlyOpenedStream, false);

            Stream resultStream = sslStream; 

            var authenticationOptions = new SslClientAuthenticationOptions()
            {
                TargetHost = authority.HostName , 
                EnabledSslProtocols = context.ProxyTlsProtocols,
                ApplicationProtocols = httpProtocols
            };
            

            if (context.ClientCertificates != null && context.ClientCertificates.Count > 0)
            {
                authenticationOptions.ClientCertificates = context.ClientCertificates;
            }

            await sslStream.AuthenticateAsClientAsync(authenticationOptions, token);

            connection.SslInfo = new SslInfo(sslStream);

            connection.SslNegotiationEnd = _timeProvider.Instant();
            connection.SslInfo.RemoteCertificate = remoteCertificate;

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