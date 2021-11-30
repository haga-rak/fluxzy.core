using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await H2Test().ConfigureAwait(false);
            Console.WriteLine("Terminé");
            Console.ReadLine();
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

                    H2ClientConnection connection = await H2ClientConnection.Open(sslStream, new H2StreamSetting());

                    var response = await connection.Send(
                        "GET https://httpwg.org/ HTTP/1.1\r\nHost: httpwg.org\r\n\r\n".AsMemory(),
                        null);

                    var responseString = await response.ResponseToString();

                }

            }

        }
    }
}
