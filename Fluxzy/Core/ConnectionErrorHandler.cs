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
    public static class ConnectionErrorConstants
    {
        public static readonly string Generic502 =
            "HTTP/1.1 528 Fluxzy error\r\n" +
            "x-fluxzy: Fluxzy error\r\n" +
            "Content-length: {0}\r\n" +
            "Content-type: text/plain\r\n" +
            "Connection : close\r\n\r\n";
    }

    public class ConnectionErrorHandler
    {
        public static bool RequalifyOnResponseSendError(
            Exception ex,
            Exchange exchange)
        {
            // Filling client error

            var remoteIpAddress = exchange.Connection?.RemoteAddress?.ToString(); 

            if (ex.TryGetException<SocketException>(out var socketException))
            {
                switch (socketException.SocketErrorCode) {
                    case SocketError.ConnectionReset:
                    {
                        var clientError = new ClientError(
                            (int) socketException.SocketErrorCode,
                            $"The connection was reset by remote peer {remoteIpAddress}") {
                            ExceptionMessage = socketException.Message
                        }; 

                        exchange.ClientErrors.Add(clientError);
                        break;
                    }
                    case SocketError.TimedOut:
                    {
                        var clientError = new ClientError(
                            (int) socketException.SocketErrorCode,
                            $"The remote peer ({remoteIpAddress}) could not be contacted within the configured timeout") 
                        {
                            ExceptionMessage = socketException.Message
                        };

                        exchange.ClientErrors.Add(clientError);
                        break;
                    }
                    case SocketError.ConnectionRefused:
                    {
                        var clientError = new ClientError(
                            (int) socketException.SocketErrorCode,
                            $"The remote peer ({remoteIpAddress}) responded but refused actively to establish a connection")
                        {
                            ExceptionMessage = socketException.Message
                        };

                        exchange.ClientErrors.Add(clientError);
                        break;
                    }
                    default:
                    {
                        var clientError = new ClientError(
                            (int)socketException.SocketErrorCode,
                            $"A socket exception has occured")
                        {
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
                ex is AuthenticationException)
            {
                var message = "Fluxzy close connection due to server connection errors.\r\n\r\n";

                if (DebugContext.EnableDumpStackTraceOn502 && exchange.Request?.Header != null)
                    message += exchange.Request.Header.GetHttp11Header().ToString();

                if (DebugContext.EnableDumpStackTraceOn502)
                    message += ex.ToString();

                if (DebugContext.EnableDumpStackTraceOn502)
                {
                    exchange.Metrics.ErrorInstant = DateTime.Now;

                    message += "\r\n" + "\r\n" + JsonSerializer.Serialize(exchange.Metrics, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }

                var messageBinary = Encoding.UTF8.GetBytes(message);

                var header = string.Format(ConnectionErrorConstants.Generic502,
                    messageBinary.Length);

                exchange.Response.Header = new ResponseHeader(
                    header.AsMemory(),
                    exchange.Authority.Secure);

                if (DebugContext.EnableDumpStackTraceOn502)
                    Console.WriteLine(message);

                exchange.Response.Body = new MemoryStream(messageBinary);

                if (!exchange.ExchangeCompletionSource.Task.IsCompleted)
                    exchange.ExchangeCompletionSource.SetResult(true);

                

                return true;
            }


            return false;
        }
    }

    public static class ExceptionUtils
    {
        /// <summary>
        /// Retrieve an inner or aggreate exception 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ex"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetException<T>(this Exception ex, out T result)
            where T : Exception
        {
            if (ex is T exception)
            {
                result = exception;
                return true;
            }

            if (ex.InnerException != null)
                return TryGetException(ex.InnerException, out result);

            if (ex is AggregateException aggregateException)
                foreach (var innerException in aggregateException.InnerExceptions)
                    if (TryGetException(innerException, out result))
                        return true;

            result = null!;
            return false;
        }

    }


    public class ClientErrorException : Exception
    {
        public ClientErrorException(int errorCode, string message, string? innerMessageException = null)
            : base(message)
        {
            ClientError = new ClientError(errorCode, message)
            {
                ExceptionMessage = innerMessageException
            };
        }

        public ClientError ClientError { get; }
    }




}
