// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core;
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

        public Connection Connection { get; }
    }

    internal class RemoteConnectionBuilder
    {
        private readonly ISslConnectionBuilder _sslConnectionBuilder;
        private readonly ITimingProvider _timeProvider;

        public RemoteConnectionBuilder(
            ITimingProvider timeProvider, ISslConnectionBuilder sslConnectionBuilder)
        {
            _timeProvider = timeProvider;
            _sslConnectionBuilder = sslConnectionBuilder;
        }

        public async ValueTask<RemoteConnectionResult> OpenConnectionToRemote(
            Exchange exchange, DnsResolutionResult resolutionResult,
            List<SslApplicationProtocol> httpProtocols,
            ProxyRuntimeSetting setting,
            ProxyConfiguration? proxyConfiguration,
            CancellationToken token)
        {
            exchange.Connection = new Connection(exchange.Authority, setting.IdProvider) {
                TcpConnectionOpening = _timeProvider.Instant(),

                // tcpClient.LingerState.
                DnsSolveStart = resolutionResult.DnsSolveStart,
                DnsSolveEnd = resolutionResult.DnsSolveEnd
            };

            exchange.Connection.RemoteAddress = resolutionResult.EndPoint.Address;

            var tcpConnection = setting.TcpConnectionProvider
                                       .Create(
                                           setting.ArchiveWriter != null!
                                           ? setting.ArchiveWriter.GetDumpfilePath(exchange.Connection.Id)!
                                           : string.Empty);
            
            var connectResult = await tcpConnection.ConnectAsync(
                resolutionResult.EndPoint.Address,
                resolutionResult.EndPoint.Port).ConfigureAwait(false);

            exchange.Connection.TcpConnectionOpened = _timeProvider.Instant();
            exchange.Connection.LocalPort = connectResult.Stream.LocalEndPoint.Port;
            exchange.Connection.LocalAddress = connectResult.Stream.LocalEndPoint.Address.ToString();

            var newlyOpenedStream = connectResult.Stream;

            if (proxyConfiguration != null) {
                exchange.Connection.ProxyConnectStart = _timeProvider.Instant();

                if (exchange.Authority.Secure) {
                    // Simulate CONNECT only when the connection is secure

                    var connectConfiguration = new ConnectConfiguration(exchange.Authority.HostName,
                        exchange.Authority.Port, proxyConfiguration.ProxyAuthorizationHeader);

                    var proxyOpenResult =
                        await UpstreamProxyManager.Connect(connectConfiguration, newlyOpenedStream, newlyOpenedStream);

                    if (proxyOpenResult != UpstreamProxyConnectResult.Ok)
                        throw new InvalidOperationException($"Failed to connect to upstream proxy {proxyOpenResult}");
                }

                exchange.Connection.ProxyConnectEnd = _timeProvider.Instant();
            }

            if (!exchange.Authority.Secure || exchange.Context.BlindMode) {
                exchange.Connection.ReadStream = exchange.Connection.WriteStream = newlyOpenedStream;

                return new RemoteConnectionResult(RemoteConnectionResultType.Unknown, exchange.Connection);
            }

            exchange.Connection.SslNegotiationStart = _timeProvider.Instant();

            byte[]? remoteCertificate = null;

            var builderOptions = new SslConnectionBuilderOptions(
                              exchange.Authority.HostName,
                              exchange.Context.ProxyTlsProtocols,
                              httpProtocols,
                              exchange.Context.SkipRemoteCertificateValidation ? (_, _, _, errors) => true : null,
                              exchange.Context.SkipRemoteCertificateValidation,
                              exchange.Context.ClientCertificates != null && exchange.Context.ClientCertificates.Any() ? exchange.Context.ClientCertificates.First() : null,
                              exchange.Context.AlwaysSendClientCertificate,
                              exchange.Context.AdvancedTlsSettings);

            var sslConnectionInfo = 
                await _sslConnectionBuilder.AuthenticateAsClient(newlyOpenedStream, builderOptions, connectResult.ProcessNssKey, token).ConfigureAwait(false);

            exchange.Connection.SslInfo = sslConnectionInfo.SslInfo;

            exchange.Connection.SslNegotiationEnd = _timeProvider.Instant();
            exchange.Connection.SslInfo.RemoteCertificate = remoteCertificate;

            exchange.Context.UnderlyingBcStream = sslConnectionInfo.UnderlyingBcStream;
            exchange.Context.EventNotifierStream = sslConnectionInfo.EventNotifierStream;

            var resultStream = sslConnectionInfo.Stream;

            if (DebugContext.EnableNetworkFileDump) {
                resultStream = new DebugFileStream($"raw/{exchange.Connection.Id:000000}_remotehost_",
                    resultStream);
            }

            var protoType = sslConnectionInfo.ApplicationProtocol == SslApplicationProtocol.Http2
                ? RemoteConnectionResultType.Http2
                : RemoteConnectionResultType.Http11;

            exchange.Connection.ReadStream = exchange.Connection.WriteStream = resultStream;

            return new RemoteConnectionResult(protoType, exchange.Connection);
        }
    }
}
