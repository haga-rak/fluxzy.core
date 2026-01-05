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

            // 1. Read and validate version byte (prepended by ProtocolDetectingSourceProvider)
            var versionByte = new byte[1];
            var versionRead = await stream.ReadAsync(versionByte, token).ConfigureAwait(false);

            if (versionRead != 1 || versionByte[0] != Socks5Constants.Version)
                throw new Socks5ProtocolException($"Invalid SOCKS version: {(versionRead == 0 ? "EOF" : versionByte[0].ToString())}");

            // 2. Read greeting (NMETHODS and METHODS)
            var methods = await Socks5ProtocolHandler.ReadGreetingAsync(stream, token).ConfigureAwait(false);

            // 3. Select and respond with auth method
            var selectedMethod = _authAdapter.SelectAuthMethod(methods);
            await Socks5ProtocolHandler.WriteMethodSelectionAsync(stream, selectedMethod, token).ConfigureAwait(false);

            if (selectedMethod == Socks5Constants.AuthNoAcceptable)
                return null;

            // 4. Handle authentication if required
            if (selectedMethod == Socks5Constants.AuthUsernamePassword)
            {
                var (username, password) = await Socks5ProtocolHandler.ReadUsernamePasswordAsync(stream, token)
                    .ConfigureAwait(false);

                var authValid = _authAdapter.ValidateCredentials(localEndpoint, remoteEndPoint, username, password);
                await Socks5ProtocolHandler.WriteAuthReplyAsync(stream, authValid, token).ConfigureAwait(false);

                if (!authValid)
                    return null;
            }

            // 5. Read connect request
            var request = await Socks5ProtocolHandler.ReadRequestAsync(stream, token).ConfigureAwait(false);

            // 6. Validate command (only CONNECT supported)
            if (request.Command != Socks5Constants.CmdConnect)
            {
                await Socks5ProtocolHandler.WriteErrorReplyAsync(
                    stream, Socks5Constants.RepCommandNotSupported, token).ConfigureAwait(false);
                return null;
            }

            // 7. Create authority from the request
            var authority = new Authority(request.DestinationAddress, request.DestinationPort, true);

            // 8. Create exchange context
            var exchangeContext = await _contextBuilder.Create(authority, true).ConfigureAwait(false);

            // 9. Send success reply before establishing the tunnel
            // Some SOCKS5 clients don't support domain name replies, so resolve to IP if needed
            var replyAddressType = request.AddressType;
            var replyRawAddress = request.RawAddress;

            if (request.AddressType == Socks5Constants.AddrTypeDomain)
            {
                var addresses = await Dns.GetHostAddressesAsync(request.DestinationAddress, token)
                    .ConfigureAwait(false);

                if (addresses.Length > 0)
                {
                    var resolvedAddress = addresses[0];
                    replyRawAddress = resolvedAddress.GetAddressBytes();
                    replyAddressType = resolvedAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                        ? Socks5Constants.AddrTypeIPv6
                        : Socks5Constants.AddrTypeIPv4;
                }
                else {
                    // If resolution fails, fall back to IPv4 with
                    replyRawAddress = new byte[Socks5Constants.IPv4AddressLength];
                    replyAddressType = Socks5Constants.AddrTypeIPv4;
                }
            }

            await Socks5ProtocolHandler.WriteReplyAsync(
                stream,
                Socks5Constants.RepSucceeded,
                replyAddressType,
                replyRawAddress,
                request.DestinationPort,
                token).ConfigureAwait(false);

            // 10. Create synthetic header for the SOCKS5 connection
            var syntheticHeaderText = CreateSyntheticConnectHeader(request);

            // 11. Handle blind mode (no decryption)
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

            // 12. Perform TLS upgrade if not blind mode
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

        private static readonly string Socks5ResponseHeader =
            "HTTP/1.1 200 Connection Established\r\n" +
            "X-Fluxzy-Protocol: SOCKS5\r\n" +
            "\r\n";

        private static string CreateSyntheticConnectHeader(Socks5Request request)
        {
            return $"CONNECT {request.DestinationAddress}:{request.DestinationPort} HTTP/1.1\r\n" +
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
