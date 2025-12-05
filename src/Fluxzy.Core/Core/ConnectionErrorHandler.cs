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

            if (exchange.Metrics.ResponseBodyEnd == default) {
                exchange.Metrics.ResponseBodyEnd = timingProvider.Instant();
            }

            var remoteIpAddress = exchange.Connection?.RemoteAddress?.ToString();

            if (ex.TryGetException<SocketException>(out var socketException)) {
                switch (socketException.SocketErrorCode) {
                    case SocketError.ConnectionReset: {
                        var clientError = new ClientError(
                            (int) socketException.SocketErrorCode,
                            $"The connection was reset by remote peer {remoteIpAddress}.") {
                            ExceptionMessage = socketException.Message
                        };

                        exchange.ClientErrors.Add(clientError);

                        break;
                    }

                    case SocketError.TimedOut: {
                        var clientError = new ClientError(
                            (int) socketException.SocketErrorCode,
                            $"The remote peer ({remoteIpAddress}) " +
                            $"could not be contacted within the configured timeout on the port {exchange.Authority.Port}.") {
                            ExceptionMessage = socketException.Message
                        };

                        exchange.ClientErrors.Add(clientError);

                        break;
                    }

                    case SocketError.ConnectionRefused: {
                        var clientError = new ClientError(
                            (int) socketException.SocketErrorCode,
                            $"The remote peer ({remoteIpAddress}) responded but refused actively to establish a connection.") {
                            ExceptionMessage = socketException.Message
                        };

                        exchange.ClientErrors.Add(clientError);

                        break;
                    }

                    default: {
                        var clientError = new ClientError(
                            (int) socketException.SocketErrorCode,
                            "A socket exception has occured") {
                            ExceptionMessage = socketException.Message
                        };

                        exchange.ClientErrors.Add(clientError);

                        break;
                    }
                }
            }

            if (ex.TryGetException<ClientErrorException>(out var clientErrorException)) {
                exchange.ClientErrors.Add(clientErrorException.ClientError);
            }
            
            if (ex.TryGetException<RuleExecutionFailureException>(out var ruleExecutionFailureException)) {
                exchange.ClientErrors.Add(new ClientError(999, ruleExecutionFailureException.Message));
            }

            if (!exchange.ClientErrors.Any()) {
                exchange.ClientErrors.Add(new ClientError(0, "A generic error has occured") {
                    ExceptionMessage = ex.Message
                });
            }

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

            exchange.ClientErrors.Add(new ClientError(9999, message));

            exchange.Response.Header = new ResponseHeader(
                header.AsMemory(),
                exchange.Authority.Secure, true);

            exchange.Response.Body = new MemoryStream(body);

            exchange.Metrics.ResponseHeaderStart = timingProvider.Instant();

            await downStreamPipe.WriteResponseHeader(exchange.Response.Header, buffer, true, exchange.StreamIdentifier, token);

            exchange.Metrics.ResponseHeaderEnd = timingProvider.Instant();
            exchange.Metrics.ResponseBodyStart = timingProvider.Instant();

            await downStreamPipe.WriteResponseBody(exchange.Response.Body, buffer, false, exchange.StreamIdentifier, token);

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
    }
}
