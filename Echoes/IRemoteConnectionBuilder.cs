// Copyright © 2021 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes
{
    /// <summary>
    /// The role of this object is to provide a remote connection
    /// </summary>
    public interface IRemoteConnectionBuilder
    {
        ValueTask<RemoteConnectionResult> OpenConnectionToRemote(Exchange exchange, bool blind,
            List<SslApplicationProtocol> httpProtocols, GlobalSetting setting, CancellationToken token); 
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
            GlobalSetting setting, 
            CancellationToken token)
        {
            var tcpClient = new TcpClient();
           
            exchange.Metrics.ConnectStart = _timeProvider.Instant();

            await tcpClient.ConnectAsync(exchange.Authority.HostName, exchange.Authority.Port).ConfigureAwait(false);

            exchange.Metrics.ConnectEnd = _timeProvider.Instant();

            var currentStream = tcpClient.GetStream();

            if (blind || !exchange.Authority.Secure)
            {
                exchange.UpStream = currentStream; 
                return  RemoteConnectionResult.Unknown;
            }

            exchange.Metrics.SslNegotiationStart = _timeProvider.Instant();

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

            exchange.Metrics.SslNegotiationEnd = _timeProvider.Instant();

            return sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2
                ? RemoteConnectionResult.Http2
                : RemoteConnectionResult.Http11; 
        }
    }
}