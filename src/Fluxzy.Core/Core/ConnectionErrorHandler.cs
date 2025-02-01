// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Rules;

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

                    //if (DebugContext.EnableDumpStackTraceOn502)
                    //    Console.WriteLine(message);

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
            ExchangeSourceInitResult? exchangeInitResult,
            Exchange? exchange,
            RsBuffer buffer,
            ITimingProvider timingProvider)
        {
            if (exchange?.Connection == null || exchangeInitResult?.WriteStream == null)
                return false;

            var message = "An unknown error has occured.";


            if (ex is RuleExecutionFailureException ruleException) {
                message = 
                    "A rule execution failure has occured.\r\n\r\n" + ruleException.Message;
            }

            if (DebugContext.EnableDumpStackTraceOn502)
            {
                message += $"\r\n" +
                           $"Stacktrace\r\n{ex}";
            }

            var (header, body) = ConnectionErrorPageHelper.GetSimplePlainTextResponse(
                exchange.Authority,
                message);


            exchange.Response.Header = new ResponseHeader(
                header.AsMemory(),
                exchange.Authority.Secure, true);

            exchange.Response.Body = new MemoryStream(body);


            var responseHeaderLength = exchange.Response.Header!
                                               .WriteHttp11(false, buffer, true, true,
                                                   true);

            exchange.Metrics.ResponseHeaderStart = timingProvider.Instant();

            await exchangeInitResult.WriteStream
                                    .WriteAsync(buffer.Buffer, 0, responseHeaderLength)
                                    .ConfigureAwait(false);

            exchange.Metrics.ResponseHeaderEnd = timingProvider.Instant();
            exchange.Metrics.ResponseBodyStart = timingProvider.Instant();

            await exchange.Response.Body.CopyToAsync(exchangeInitResult.WriteStream)
                          .ConfigureAwait(false);

            if (exchange.Metrics.ResponseBodyEnd == default)
            {
                exchange.Metrics.ResponseBodyEnd = timingProvider.Instant();
            }

            if (!exchange.ExchangeCompletionSource.Task.IsCompleted)
            {
                exchange.ExchangeCompletionSource.TrySetResult(true);
            }

            return true;
        }
    }
}
