using System.Net.Http;
using System.Text;

namespace Echoes.DotNetBridge
{
    public static class HttpRequestMessageExtensions
    {
        public static string ToHttp11String(this HttpRequestMessage message)
        {
            var builder = new StringBuilder();

            builder.Append($"{message.Method} {message.RequestUri} HTTP/1.1\r\n");
            builder.Append($"Host: {message.RequestUri.Authority}\r\n");

            foreach (var header in message.Headers)
            {
                foreach (var value in header.Value)
                {
                    builder.Append(header.Key);
                    builder.Append(": ");
                    builder.Append(value);
                    builder.Append("\r\n");
                }
            }

            var clAsk = message?.Content?.Headers.ContentLength;

            if (message.Content?.Headers != null)
            {
                foreach (var header in message.Content.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        builder.Append(header.Key);
                        builder.Append(": ");
                        builder.Append(value);
                        builder.Append("\r\n");
                    }
                }
            }

            var s = message.ToString();

            builder.Append("\r\n");
            var yo = builder.ToString();

            return yo; 
        }
    }
}
