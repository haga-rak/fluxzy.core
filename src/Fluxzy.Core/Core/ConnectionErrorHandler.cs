// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;

namespace Fluxzy.Core
{
    public class ConnectionErrorHandler
    {
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

            if (!exchange.ClientErrors.Any()) {
                exchange.ClientErrors.Add(new ClientError(0, "A generic error has occured") {
                    ExceptionMessage = ex.Message
                });
            }

            if (ex is SocketException ||
                ex is IOException ||
                ex is H2Exception ||
                ex is ClientErrorException ||
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

                        message += "\r\n" + "\r\n" + JsonSerializer.Serialize(exchange.Metrics,
                            new JsonSerializerOptions {
                                WriteIndented = true
                            });
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
            }
        }
