using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2
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
                var host = "extranet.2befficient.fr";

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

                    await sslStream.AuthenticateAsClientAsync(sslAuthenticationOption, CancellationToken.None).ConfigureAwait(false);

                    H2ClientConnection connection = await H2ClientConnection.Open(sslStream, new H2StreamSetting());

                    while (true)
                    {
                        using var response1 = await connection.Send(
                            "GET https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ HTTP/1.1\r\nHost: extranet.2befficient.fr\r\n\r\n".AsMemory(),
                            null);

                        using var response2 = await connection.Send(
                            "GET https://extranet.2befficient.fr/Content/Global/Mandatory/Plugins?v=1N0a7hp5TbdWJTlXvMrIuuR-xx1KyQWnGp2I80A7L1I1 HTTP/1.1\r\nHost: extranet.2befficient.fr\r\n\r\n".AsMemory(),
                            null);
                    }
                }

            }

        }
    }
}
