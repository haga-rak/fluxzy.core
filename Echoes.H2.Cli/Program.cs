using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await H2Test().ConfigureAwait(false);
        }


        static async Task H2Test()
        {
            using (var tcpClient = new TcpClient())
            {
                var host = "httpwg.org";

                await tcpClient.ConnectAsync(host, 443).ConfigureAwait(false);

                using (SslStream sslStream = new SslStream(tcpClient.GetStream()))
                {
                    var sslAuthenticationOption = new SslClientAuthenticationOptions()
                    {
                        TargetHost = host,
                        ApplicationProtocols = new List<SslApplicationProtocol>()
                        {
                            SslApplicationProtocol.Http2,
                            SslApplicationProtocol.Http11,
                        }
                    };

                    await sslStream.AuthenticateAsClientAsync(sslAuthenticationOption);

                    byte [] buffer = new byte[1024];


                    await sslStream.WriteAsync(Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"));

                   // var n = await sslStream.ReadAsync(buffer, 0, buffer.Length);


                   while (true)
                   {
                       var frame = await H2Reader.ReadNextFrameAsync(sslStream);
                    }


                  //  var str = Encoding.UTF8.GetString(buffer, 0, n);


                    Console.WriteLine(sslStream.NegotiatedApplicationProtocol);

                }

            }

        }
    }


    public class H2FrameReadResult
    {
        public H2FrameReadResult(H2Frame header, IFixedSizeFrame payload)
        {
            Header = header;
            Payload = payload;
        }

        public H2Frame Header { get;  }

        public IFixedSizeFrame Payload { get;  }
    }

    
    public static class StreamReadHelper
    {
        public static async Task ReadExact(this Stream origin, byte[] buffer, int offset, int length)
        {
            int readen = 0;
            int currentIndex = offset;
            int remain = length; 

            while (readen < length)
            {
                var currentReaden = await origin.ReadAsync(buffer, currentIndex, remain);

                if (currentReaden <= 0)
                    throw new InvalidOperationException($"Stream does not have {length} octets");

                currentIndex += currentReaden; 
                remain -= currentReaden; 
                readen += (currentReaden); 
            }
        }
    }
}
