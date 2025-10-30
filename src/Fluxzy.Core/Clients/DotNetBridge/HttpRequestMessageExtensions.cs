// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Text;

namespace Fluxzy.Clients.DotNetBridge
{
    /// <summary>
    /// Request message extensions
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Returns a flat HTTP/1.1 string representation of the request message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string ToHttp11String(this HttpRequestMessage message)
        {
            StringBuilder builder = new();

            builder.Append($"{message.Method} {message.RequestUri} HTTP/1.1\r\n");
            builder.Append($"Host: {message.RequestUri!.Authority}\r\n");

            builder.Append(message.Headers.ToString());

            // Do not remove that line because evaluating ContentLength is necessary
            _ = message?.Content?.Headers.ContentLength;

            if (message!.Content?.Headers != null)
            {
                builder.Append(message.Content.Headers.ToString());
            }

            builder.Append("\r\n");

            return builder.ToString();
        }
    }
}
