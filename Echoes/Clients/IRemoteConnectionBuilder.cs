// Copyright © 2021 Haga Rakotoharivelo

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.IO;

namespace Echoes
{
    public enum RemoteConnectionResultType : byte
    {
        Unknown = 0,
        Http11,
        Http2
    }

    public class RemoteConnectionResult
    {
        public RemoteConnectionResult(RemoteConnectionResultType type, Stream openedStream, Connection connection)
        {
            Type = type;
            OpenedStream = openedStream;
            Connection = connection;
        }

        public RemoteConnectionResultType Type { get; }

        public Stream OpenedStream { get;  }

        public Connection Connection { get;  }
    }

    public class RemoteConnectionBuilder
    {
        private readonly ITimingProvider _timeProvider;

        public RemoteConnectionBuilder(ITimingProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public async ValueTask<RemoteConnectionResult> OpenConnectionToRemote(
            Authority authority, 
            bool blind,
            List<SslApplicationProtocol> httpProtocols,
            ClientSetting setting, 
            CancellationToken token)
        {
            var tcpClient = new TcpClient();

            var connection = new Connection(authority)
            {
                TcpConnectionOpening = _timeProvider.Instant()
            };

            await tcpClient.ConnectAsync(authority.HostName, authority.Port).ConfigureAwait(false);

            connection.TcpConnectionOpened = _timeProvider.Instant();

            var newlyOpenedStream = tcpClient.GetStream();
            
            if (!authority.Secure)
            {
                return new RemoteConnectionResult(RemoteConnectionResultType.Unknown, newlyOpenedStream, connection);
            }

            connection.SslNegotiationStart = _timeProvider.Instant();

            var sslStream = new SslStream(newlyOpenedStream, false, setting.CertificateValidationCallback);
            Stream outStream = sslStream; 

            SslClientAuthenticationOptions authenticationOptions = new SslClientAuthenticationOptions()
            {
                ClientCertificates = setting.GetCertificateByHost(authority.HostName), 
                TargetHost = authority.HostName , 
                EnabledSslProtocols = setting.ProxyTlsProtocols,
                ApplicationProtocols = httpProtocols
            };

            await sslStream.AuthenticateAsClientAsync(authenticationOptions, token).ConfigureAwait(false);

            connection.SslNegotiationEnd = _timeProvider.Instant();

            if (DebugContext.EnableNetworkFileDump)
            {
                outStream = new DebugFileStream($"raw/{connection.Id:000000}_remotehost_",
                    outStream); 
            }

            var protoType =  sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2
                ? RemoteConnectionResultType.Http2
                : RemoteConnectionResultType.Http11;

            return new RemoteConnectionResult(protoType, outStream, connection);
        }
    }
}