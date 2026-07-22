// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Rules;
using Fluxzy.Writers;

namespace Fluxzy.Core
{
    internal static class ConnectionErrorHandler
    {
        private static readonly JsonSerializerOptions PrettyJsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public static bool RequalifyOnResponseSendError(
            Exception ex,
            Exchange exchange, ITimingProvider timingProvider)
        {
            // Filling client error

            var extraHeaders = new StringBuilder();

            if (exchange.Metrics.ResponseBodyEnd == default) {
                exchange.Metrics.ResponseBodyEnd = timingProvider.Instant();
            }

            var remoteIpAddress = exchange.Connection?.RemoteAddress?.ToString();

            if (ex.TryGetException<SocketException>(out var socketException)) {
                var (message, socketErrorToken) = MapSocketError(
                    socketException.SocketErrorCode, remoteIpAddress, exchange.Authority.Port);

                exchange.ClientErrors.Add(new ClientError(
                    (int) socketException.SocketErrorCode, message, socketErrorToken) {
                    ExceptionMessage = socketException.Message
                });
            }

            if (ex.TryGetException<ClientErrorException>(out var clientErrorException)) {
                exchange.ClientErrors.Add(clientErrorException.ClientError);
            }

            if (ex.TryGetException<RuleExecutionFailureException>(out var ruleExecutionFailureException)) {
                exchange.ClientErrors.Add(new ClientError(999, ruleExecutionFailureException.Message,
                    NetworkErrorCodes.RuleFailure));
            }

            if (!exchange.ClientErrors.Any()) {

                extraHeaders.Append($"x-fluxzy-error-code: 0\r\n");
                extraHeaders.Append($"x-fluxzy-error-message: {ExceptionUtils.SanitizeHeaderValue(ex.Message)}\r\n");


                exchange.ClientErrors.Add(new ClientError(0, ex.Message, ResolveNetworkErrorCode(ex)) {
                    ExceptionMessage = ex.Message
                });
            }

            // Always emit x-fluxzy-network-error so consumers can react programmatically.
            var networkErrorCode = exchange.ClientErrors
                                           .Select(e => e.NetworkErrorCode)
                                           .FirstOrDefault(code => !string.IsNullOrEmpty(code))
                                  ?? ResolveNetworkErrorCode(ex);

            extraHeaders.Append($"x-fluxzy-network-error: {networkErrorCode}\r\n");

            if (ex is SocketException ||
                ex is IOException ||
                ex is H2Exception ||
                ex is ClientErrorException ||
                ex is RuleExecutionFailureException ||
                ex is AuthenticationException) {
                if (DebugContext.EnableDumpStackTraceOn502) {
                    var message = "Fluxzy close connection due to server connection errors.\r\n\r\n";

                    if (DebugContext.EnableDumpStackTraceOn502 && exchange.Request?.Header != null) {
                        message += exchange.Request.Header.GetHttp11Header().ToString();
                    }

                    if (DebugContext.EnableDumpStackTraceOn502) {
                        message += ex.ToString();
                    }

                    if (DebugContext.EnableDumpStackTraceOn502) {
                        exchange.Metrics.ErrorInstant = DateTime.Now;
                        message += "\r\n" + "\r\n" + JsonSerializer.Serialize(exchange.Metrics,PrettyJsonOptions);
                    }

                    var messageBinary = Encoding.UTF8.GetBytes(message);

                    var header = string.Format(ConnectionErrorConstants.Generic502,
                        messageBinary.Length);

                    if (extraHeaders.Length > 0) {
                        // Insert after the final header line's CRLF, before the empty-line CRLF
                        // that separates headers from body — otherwise the new header gets
                        // glued onto the tail of the previous one and the parser merges them.
                        var idx = header.IndexOf("\r\n\r\n", StringComparison.Ordinal);

                        header = header.Insert(idx + 2, extraHeaders.ToString());
                    }

                    exchange.Response.Header = new ResponseHeader(
                        header.AsMemory(),
                        exchange.Authority.Secure, true);

                    exchange.Response.Body = new MemoryStream(messageBinary);

                    if (!exchange.ExchangeCompletionSource.Task.IsCompleted) {
                        exchange.ExchangeCompletionSource.TrySetResult(true);
                    }

                    return true;
                }
                else {
                    var (header, body) = ConnectionErrorPageHelper.GetPrettyErrorPage(
                        exchange.Authority,
                        exchange.ClientErrors,
                        ex);

                    if (extraHeaders.Length > 0)
                    {
                        var idx = header.IndexOf("\r\n\r\n", StringComparison.Ordinal);

                        header = header.Insert(idx + 2, extraHeaders.ToString());
                    }

                    exchange.Response.Header = new ResponseHeader(
                        header.AsMemory(),
                        exchange.Authority.Secure, true);

                    exchange.Response.Body = new MemoryStream(body);

                    if (!exchange.ExchangeCompletionSource.Task.IsCompleted) {
                        exchange.ExchangeCompletionSource.TrySetResult(true);
                    }

                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> HandleGenericException(Exception ex,
            IDownStreamPipe? downStreamPipe,
            Exchange? exchange,
            RsBuffer buffer,
            RealtimeArchiveWriter? archiveWriter,
            ITimingProvider timingProvider, CancellationToken token)
        {
            if (exchange?.Connection == null || downStreamPipe == null || !downStreamPipe.CanWrite)
                return false;

            var message = "A configuration error has occured.\r\n";

            if (ex is RuleExecutionFailureException ruleException) {
                message =
                    "A rule execution failure has occured.\r\n\r\n" + ruleException.Message;
            }

            if (DebugContext.EnableDumpStackTraceOn502)
            {
                message += $"\r\n" +
                           $"Stacktrace:\r\n{ex}";
            }

            var (header, body) = ConnectionErrorPageHelper.GetSimplePlainTextResponse(
                exchange.Authority,
                message, ex.ToString());

            var networkErrorCode = ex is RuleExecutionFailureException
                ? NetworkErrorCodes.RuleFailure
                : NetworkErrorCodes.Unknown;

            var endOfHeaders = header.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            header = header.Insert(endOfHeaders + 2, $"x-fluxzy-network-error: {networkErrorCode}\r\n");

            exchange.ClientErrors.Add(new ClientError(9999, message, networkErrorCode));

            exchange.Response.Header = new ResponseHeader(
                header.AsMemory(),
                exchange.Authority.Secure, true);

            exchange.Response.Body = new MemoryStream(body);

            exchange.Metrics.ResponseHeaderStart = timingProvider.Instant();

            await downStreamPipe.WriteResponseHeader(exchange.Response.Header, buffer, true, exchange.StreamIdentifier, exchange.Request.Header.Method, token);

            exchange.Metrics.ResponseHeaderEnd = timingProvider.Instant();
            exchange.Metrics.ResponseBodyStart = timingProvider.Instant();

            await downStreamPipe.WriteResponseBody(exchange.Response.Body, buffer, false, exchange.StreamIdentifier, exchange.Response, token);

            if (exchange.Metrics.ResponseBodyEnd == default)
            {
                exchange.Metrics.ResponseBodyEnd = timingProvider.Instant();
            }

            if (!exchange.ExchangeCompletionSource.Task.IsCompleted)
            {
                exchange.ExchangeCompletionSource.TrySetResult(true);
            }

            archiveWriter?.Update(exchange, ArchiveUpdateType.AfterResponse, token);


            return true;
        }

        internal static string ResolveNetworkErrorCode(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException) {
                if (current is ClientErrorException cee && !string.IsNullOrEmpty(cee.ClientError.NetworkErrorCode)) {
                    return cee.ClientError.NetworkErrorCode!;
                }

                if (current is RuleExecutionFailureException) {
                    return NetworkErrorCodes.RuleFailure;
                }

                if (current is AuthenticationException) {
                    return NetworkErrorCodes.TlsHandshakeFailure;
                }
            }

            return NetworkErrorCodes.Unknown;
        }

        internal static (string Message, string NetworkErrorCode) MapSocketError(
            SocketError code, string? remoteIpAddress, int port)
        {
            return code switch {
                SocketError.ConnectionReset => (
                    $"The connection was reset by remote peer {remoteIpAddress}.",
                    NetworkErrorCodes.ConnectionReset),

                // EPIPE on Linux: peer sent RST (or closed) and we then tried to write.
                // .NET surfaces this as SocketError.Shutdown — semantically a reset by peer.
                SocketError.Shutdown => (
                    $"The connection was reset by remote peer {remoteIpAddress}.",
                    NetworkErrorCodes.ConnectionReset),

                // ENOTCONN: the socket was torn down by a peer reset before the
                // operation (getpeername, shutdown, …) could run.
                SocketError.NotConnected => (
                    $"The connection was reset by remote peer {remoteIpAddress}.",
                    NetworkErrorCodes.ConnectionReset),

                SocketError.TimedOut => (
                    $"The remote peer ({remoteIpAddress}) " +
                    $"could not be contacted within the configured timeout on the port {port}.",
                    NetworkErrorCodes.ConnectionTimeout),

                SocketError.ConnectionRefused => (
                    $"The remote peer ({remoteIpAddress}) responded but refused actively to establish a connection.",
                    NetworkErrorCodes.ConnectionRefused),

                SocketError.ConnectionAborted => (
                    $"The connection was aborted by remote peer {remoteIpAddress}.",
                    NetworkErrorCodes.ConnectionAborted),

                SocketError.HostUnreachable => (
                    $"The remote host ({remoteIpAddress}) is unreachable.",
                    NetworkErrorCodes.HostUnreachable),

                SocketError.NetworkUnreachable => (
                    "The remote network is unreachable.",
                    NetworkErrorCodes.NetworkUnreachable),

                SocketError.HostNotFound => (
                    "DNS lookup failed: host not found.",
                    NetworkErrorCodes.DnsNotFound),

                SocketError.TryAgain => (
                    "DNS lookup failed.",
                    NetworkErrorCodes.DnsTryAgain),

                SocketError.NoData => (
                    "DNS lookup failed.",
                    NetworkErrorCodes.DnsNoData),

                _ => (
                    "A socket exception has occured",
                    NetworkErrorCodes.Unknown)
            };
        }
    }
}
