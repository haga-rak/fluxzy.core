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
    /// <summary>
    /// The role of this object is to provide a remote connection
    /// </summary>
    public interface IRemoteConnectionBuilder
    {
        ValueTask<RemoteConnectionResult> OpenConnectionToRemote(Exchange exchange, bool blind,
            List<SslApplicationProtocol> httpProtocols, ClientSetting setting, CancellationToken token); 
    }

    public enum RemoteConnectionResult : byte
    {
        Unknown = 0,
        Http11,
        Http2
    }

    public class RemoteConnectionBuilder : IRemoteConnectionBuilder
    {
        


        private readonly ITimingProvider _timeProvider;

        public RemoteConnectionBuilder(ITimingProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public async ValueTask<RemoteConnectionResult> OpenConnectionToRemote(
            Exchange exchange,
            bool blind,
            List<SslApplicationProtocol> httpProtocols,
            ClientSetting setting, 
            CancellationToken token)
        {
            var tcpClient = new TcpClient();

            exchange.Connection = new Connection(exchange.Authority);
            exchange.Connection.TcpConnectionOpening = _timeProvider.Instant();

            await tcpClient.ConnectAsync(exchange.Authority.HostName, exchange.Authority.Port).ConfigureAwait(false);

            exchange.Connection.TcpConnectionOpened = _timeProvider.Instant();

            var currentStream = tcpClient.GetStream();

            if (blind || !exchange.Authority.Secure)
            {
                exchange.UpStream = currentStream;

                return  RemoteConnectionResult.Unknown;
            }

            exchange.Connection.SslNegotiationStart = _timeProvider.Instant();

            var sslStream = new SslStream(currentStream, true, setting.CertificateValidationCallback);

            exchange.UpStream = sslStream;

            SslClientAuthenticationOptions authenticationOptions = new SslClientAuthenticationOptions()
            {
                ClientCertificates = setting.GetCertificateByHost(exchange.Authority.HostName), 
                TargetHost = exchange.Authority.HostName , 
                EnabledSslProtocols = setting.ProxyTlsProtocols,
                ApplicationProtocols = httpProtocols
            };

            await sslStream.AuthenticateAsClientAsync(authenticationOptions, token).ConfigureAwait(false);

            exchange.Connection.SslNegotiationEnd = _timeProvider.Instant();

            if (DebugContext.EnableNetworkFileDump)
            {
                exchange.UpStream = new DebugFileStream($"raw/{exchange.Id:0000}_remotehost_",
                    exchange.UpStream); 
            }

            return sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2
                ? RemoteConnectionResult.Http2
                : RemoteConnectionResult.Http11; 
        }
    }
}