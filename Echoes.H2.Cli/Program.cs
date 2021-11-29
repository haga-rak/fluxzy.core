using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
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

                    await sslStream.AuthenticateAsClientAsync(sslAuthenticationOption).ConfigureAwait(false);



                    byte [] buffer = new byte[1024];
                    
                    var channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions()
                    {
                        
                    });
                    



                    await sslStream.WriteAsync(Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n")).ConfigureAwait(false);

                   // var n = await sslStream.ReadAsync(buffer, 0, buffer.BodyLength);


                   while (true)
                   {
                      // var frame = await H2Reader.ReadNextFrameAsync(sslStream).ConfigureAwait(false);
                    }


                  //  var str = Encoding.UTF8.GetString(buffer, 0, n);


                    Console.WriteLine(sslStream.NegotiatedApplicationProtocol);

                }

            }

        }
    }
}
