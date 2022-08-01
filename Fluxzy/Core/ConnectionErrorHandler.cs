﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Core
{
    public static class ConnectionErrorConstants
    {
        public static readonly string Generic502 =
            "HTTP/1.1 502 Bad gateway\r\n" +
            "Content-length: {0}\r\n" +
            "Content-type: text/plain" +
            "Connection : close\r\n\r\n";
    }

    public class ConnectionErrorHandler
    {
        private static readonly Http11Parser Parser = new(4096);

        public static bool RequalifyOnResponseSendError(
            Exception ex, 
            Exchange exchange )
        {
            if (ex is SocketException sex ||
                ex is IOException ioEx ||
                ex is H2Exception hEx ||
                ex is AuthenticationException aEx
                )
            {
                var message = $"Fluxzy close connection due to server connection errors.\r\n\r\n";

                if (DebugContext.EnableDumpStackTraceOn502 && exchange.Request?.Header != null)
                    message += exchange.Request.Header.RawHeader.ToString();

                if (DebugContext.EnableDumpStackTraceOn502)
                    message += ex.ToString();

                var messageBinary = Encoding.UTF8.GetBytes(message);

                var header = string.Format(ConnectionErrorConstants.Generic502,
                    messageBinary.Length);

                exchange.Response.Header = new ResponseHeader(
                    header.AsMemory(),
                    exchange.Authority.Secure,
                    Parser);

                exchange.Response.Body = new MemoryStream(messageBinary);

                if (!exchange.ExchangeCompletionSource.Task.IsCompleted)
                    exchange.ExchangeCompletionSource.SetResult(true);


                return true; 
            }

            return false; 
        }
    }
}