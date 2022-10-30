using System;
using System.IO;
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
            if (ex is SocketException sex ||
                ex is IOException ioEx ||
                ex is H2Exception hEx ||
                ex is AuthenticationException aEx
               )
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
}
