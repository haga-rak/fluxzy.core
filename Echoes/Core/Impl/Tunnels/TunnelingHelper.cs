using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Echoes.Core.Utils;

namespace Echoes.Core
{
    internal class TunnelingHelper
    {
        private static readonly byte[] AcceptTunnelResponse;

        static TunnelingHelper()
        {
            AcceptTunnelResponse = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-length: 0\r\nConnection: Keep-alive\r\n\r\n");
        }

        public static async Task AcceptTunnel(IDownStreamConnection downStreamConnection)
        {
            try
            {
                await downStreamConnection.WriteStream.WriteAsync(AcceptTunnelResponse, 0, AcceptTunnelResponse.Length)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SocketException)
                {
                    throw new EchoesException("Client closed connection before accepting tunnel", ex);
                }

                throw;
            }
        }

        public static async Task<bool> CheckForWebSocketRequest(IDownStreamConnection downStreamConnection, string knownHost, int knownPort)
        {
            byte [] buffer = new byte[4];

            var isWebSocket = false; 

            var readen = await downStreamConnection.ReadStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

            if (readen > 3) 
            {
                var firstString = Encoding.ASCII.GetString(buffer, 0, readen);
                isWebSocket = firstString.StartsWith("GET", StringComparison.OrdinalIgnoreCase);
            }

            var memoryStream = new MemoryStream(buffer, 0, readen);

            memoryStream.Seek(0, SeekOrigin.Begin);

            downStreamConnection.UpgradeReadStream(new CombinedReadonlyStream(
                new[]
                {
                    memoryStream,
                    downStreamConnection.ReadStream}, true
               ), knownHost, knownPort);
            

            return isWebSocket; 
        }
    }
}