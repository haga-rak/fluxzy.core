// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Core.Socks5
{
    /// <summary>
    /// Exchange source provider for SOCKS5 protocol.
    /// Handles SOCKS5 handshake and creates an exchange tunnel for the connection.
    /// </summary>
    internal class Socks5SourceProvider : ExchangeSourceProvider
    {
        private readonly SecureConnectionUpdater _secureConnectionUpdater;
        private readonly IIdProvider _idProvider;
        private readonly Socks5AuthenticationAdapter _authAdapter;
        private readonly IExchangeContextBuilder _contextBuilder;

        public Socks5SourceProvider(
            SecureConnectionUpdater secureConnectionUpdater,
            IIdProvider idProvider,
            ProxyAuthenticationMethod proxyAuthenticationMethod,
            IExchangeContextBuilder contextBuilder)
            : base(idProvider)
        {
            _secureConnectionUpdater = secureConnectionUpdater;
            _idProvider = idProvider;
            _authAdapter = new Socks5AuthenticationAdapter(proxyAuthenticationMethod);
            _contextBuilder = contextBuilder;
        }

        public override async ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer,
            IPEndPoint localEndpoint,
            IPEndPoint remoteEndPoint,
            CancellationToken token)
        {
            try
            {
                return await InternalInitClientConnection(stream, localEndpoint, remoteEndPoint, token)
                    .ConfigureAwait(false);
            }
            catch (Socks5ProtocolException)
            {
                return null;
            }
        }

        private async ValueTask<ExchangeSourceInitResult?> InternalInitClientConnection(
            Stream stream,
            IPEndPoint localEndpoint,
            IPEndPoint remoteEndPoint,
            CancellationToken token)
        {
            var receivedFromProxy = ITimingProvider.Default.Instant();

            // 1. Read greeting (first byte 0x05 already consumed, we read NMETHODS and METHODS)
            var methods = await Socks5ProtocolHandler.ReadGreetingAsync(stream, token).ConfigureAwait(false);

            // 2. Select and respond with auth method
            var selectedMethod = _authAdapter.SelectAuthMethod(methods);
            await Socks5ProtocolHandler.WriteMethodSelectionAsync(stream, selectedMethod, token).ConfigureAwait(false);

            if (selectedMethod == Socks5Constants.AuthNoAcceptable)
                return null;

            // 3. Handle authentication if required
            if (selectedMethod == Socks5Constants.AuthUsernamePassword)
            {
                var (username, password) = await Socks5ProtocolHandler.ReadUsernamePasswordAsync(stream, token)
                    .ConfigureAwait(false);

                var authValid = _authAdapter.ValidateCredentials(localEndpoint, remoteEndPoint, username, password);
                await Socks5ProtocolHandler.WriteAuthReplyAsync(stream, authValid, token).ConfigureAwait(false);

                if (!authValid)
                    return null;
            }

            // 4. Read connect request
            var request = await Socks5ProtocolHandler.ReadRequestAsync(stream, token).ConfigureAwait(false);

            // 5. Validate command (only CONNECT supported)
            if (request.Command != Socks5Constants.CmdConnect)
            {
                await Socks5ProtocolHandler.WriteErrorReplyAsync(
                    stream, Socks5Constants.RepCommandNotSupported, token).ConfigureAwait(false);
                return null;
            }

            // 6. Create authority from the request
            var authority = new Authority(request.DestinationAddress, request.DestinationPort, true);

            // 7. Create exchange context
            var exchangeContext = await _contextBuilder.Create(authority, true).ConfigureAwait(false);

            // 8. Send success reply before establishing the tunnel
            await Socks5ProtocolHandler.WriteReplyAsync(
                stream,
                Socks5Constants.RepSucceeded,
                request.AddressType,
                request.RawAddress,
                request.DestinationPort,
                token).ConfigureAwait(false);

            // 9. Create synthetic header for the SOCKS5 connection
            var syntheticHeaderText = CreateSyntheticConnectHeader(request);

            // 10. Handle blind mode (no decryption)
            if (exchangeContext.BlindMode)
            {
                var blindExchange = Exchange.CreateUntrackedExchange(
                    _idProvider,
                    exchangeContext,
                    authority,
                    syntheticHeaderText.AsMemory(),
                    Stream.Null,
                    Socks5ResponseHeader.AsMemory(),
                    Stream.Null,
                    false,
                    "SOCKS5",
                    receivedFromProxy);

                blindExchange.Unprocessed = false;

                return new ExchangeSourceInitResult(
                    new Http11DownStreamPipe(_idProvider, authority, stream, stream, _contextBuilder),
                    blindExchange);
            }

            // 11. Perform TLS upgrade if not blind mode
            var certStart = ITimingProvider.Default.Instant();
            var authenticateResult = await _secureConnectionUpdater.AuthenticateAsServer(
                stream, authority.HostName, exchangeContext, token).ConfigureAwait(false);
            var certEnd = ITimingProvider.Default.Instant();

            authority = new Authority(authority.HostName, authority.Port, authenticateResult.IsSsl);

            var exchange = Exchange.CreateUntrackedExchange(
                _idProvider,
                exchangeContext,
                authority,
                syntheticHeaderText.AsMemory(),
                Stream.Null,
                Socks5ResponseHeader.AsMemory(),
                Stream.Null,
                authenticateResult.IsSsl,
                "SOCKS5",
                receivedFromProxy);

            exchange.Metrics.CreateCertStart = certStart;
            exchange.Metrics.CreateCertEnd = certEnd;

            return new ExchangeSourceInitResult(
                new Http11DownStreamPipe(
                    _idProvider,
                    authority,
                    authenticateResult.InStream,
                    authenticateResult.OutStream,
                    _contextBuilder),
                exchange);
        }

        private static readonly string Socks5ResponseHeader = "SOCKS5/1.0 200 Connection established\r\n\r\n";

        private static string CreateSyntheticConnectHeader(Socks5Request request)
        {
            return $"CONNECT {request.DestinationAddress}:{request.DestinationPort} SOCKS5/1.0\r\n" +
                   $"Host: {request.DestinationAddress}:{request.DestinationPort}\r\n" +
                   $"X-Socks5-Address-Type: {GetAddressTypeName(request.AddressType)}\r\n" +
                   "\r\n";
        }

        private static string GetAddressTypeName(byte addressType)
        {
            return addressType switch
            {
                Socks5Constants.AddrTypeIPv4 => "IPv4",
                Socks5Constants.AddrTypeDomain => "Domain",
                Socks5Constants.AddrTypeIPv6 => "IPv6",
                _ => "Unknown"
            };
        }
    }
}
