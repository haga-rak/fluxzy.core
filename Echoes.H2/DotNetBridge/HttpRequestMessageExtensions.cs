using System.Net.Http;
using System.Text;

namespace Echoes.H2.DotNetBridge
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

            builder.Append("\r\n");
            return builder.ToString();
        }
    }
}
