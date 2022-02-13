using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Echoes.Core.Utils;

namespace Echoes.Core
{
    internal class ConnectErrorHelper
    {
        static readonly string ErrorTemplate =
            "HTTP/1.1 502 Bad gateway\r\n" +
            "Content-Length: {0}\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Connection: close\r\n" +
            "\r\n{1}";

        public static async Task WriteError(IDownStreamConnection connection, Exception ex)
        {
            try
            {
                var message = ex.Message;
                var payload = string.Format(ErrorTemplate, message.Length, message);
                await connection.WriteStream.WriteAsyncNS2(Encoding.UTF8.GetBytes(payload)).ConfigureAwait(false);
                
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}