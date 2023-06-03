// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Headers;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;

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
                                       .Create(setting.ArchiveWriter != null
                                           ? setting.ArchiveWriter?.GetDumpfilePath(exchange.Connection.Id)!
                                           : string.Empty);

            var localEndpoint = await tcpConnection.ConnectAsync(resolutionResult.EndPoint.Address,
                                                       resolutionResult.EndPoint.Port)
                                                   .ConfigureAwait(false);

            exchange.Connection.TcpConnectionOpened = _timeProvider.Instant();
            exchange.Connection.LocalPort = localEndpoint.Port;
            exchange.Connection.LocalAddress = localEndpoint.Address.ToString();

            var newlyOpenedStream = tcpConnection.GetStream();

            if (!exchange.Authority.Secure || exchange.Context.BlindMode) {
                exchange.Connection.ReadStream = exchange.Connection.WriteStream = newlyOpenedStream;

                return new RemoteConnectionResult(RemoteConnectionResultType.Unknown, exchange.Connection);
            }

            exchange.Connection.SslNegotiationStart = _timeProvider.Instant();

            byte[]? remoteCertificate = null;

            var authenticationOptions = new SslClientAuthenticationOptions {
                TargetHost = exchange.Authority.HostName,
                EnabledSslProtocols = exchange.Context.ProxyTlsProtocols,
                ApplicationProtocols = httpProtocols
            };

            if (exchange.Context.SkipRemoteCertificateValidation)
                authenticationOptions.RemoteCertificateValidationCallback = (_, _, _, errors) => true;

            if (exchange.Context.ClientCertificates != null && exchange.Context.ClientCertificates.Count > 0)
                authenticationOptions.ClientCertificates = exchange.Context.ClientCertificates;

            var sslConnectionInfo =
                await _sslConnectionBuilder.AuthenticateAsClient(
                    newlyOpenedStream, authenticationOptions, tcpConnection.OnKeyReceived, token);

            exchange.Connection.SslInfo = sslConnectionInfo.SslInfo;

            exchange.Connection.SslNegotiationEnd = _timeProvider.Instant();
            exchange.Connection.SslInfo.RemoteCertificate = remoteCertificate;

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
