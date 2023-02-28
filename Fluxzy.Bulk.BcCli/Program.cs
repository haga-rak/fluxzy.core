using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using Fluxzy.Clients.Ssl.BouncyCastle;
using Fluxzy.Core;
using Fluxzy.Interop.Pcap;
using Fluxzy.Interop.Pcap.Cli.Clients;
using Org.BouncyCastle.Security;

namespace Fluxzy.Bulk.BcCli
{
    /// <summary>
    /// Check list ALPN
    /// Client Certificate (OK) 
    /// Master KEY NOn (OK pour client random et server random )
    /// </summary>
    internal class Program
    {
        static async Task Main()
        {
            var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(), 
                (a) => new OutOfProcessCaptureContext(a));

            await using var tcpProvider = await CapturedTcpConnectionProvider.Create(scope, false);
            
            var uriRaw
                = "https://www.google.com/";
            if (!Uri.TryCreate(uriRaw, UriKind.Absolute, out var uri)) {
                throw new Exception("Invalid URI");
            }

            var connection = tcpProvider.Create("test.pcap");

            var ipAdress = (await Dns.GetHostAddressesAsync(uri.Host)).First();

            var endPoint = await connection.ConnectAsync(ipAdress, uri.Port);

            //ProtocolName.Http_2_Tls

            var entireRequest = $"GET {uri.PathAndQuery} HTTP/1.1\r\n" +
                                $"Host: {uri.Host}\r\n" +
                                $"User-Agent: Coco\r\n" +
                                $"Accept: text/plain\r\n" +
                                $"Connection: close\r\n" +
                                $"X-Header-Popo: Dodo\r\n\r\n";

            var stream = connection.GetStream();

            var crypto = new FluxzyCrypto();

            using var nssWriter = new NssLogWriter("ssl.txt");
            
            var cl = new FluxzyTlsClient(SslProtocols.Tls12, new[] { SslApplicationProtocol.Http11, });
   
            var protocol = new FluxzyClientProtocol(stream, nssWriter); 

            protocol.Connect(cl);
            
            stream = protocol.Stream; 

            await stream.WriteAsync(Encoding.ASCII.GetBytes(entireRequest));
            await stream.FlushAsync();

            var streamReader = new StreamReader(stream);

            try {
                var response = await streamReader.ReadToEndAsync();

                Console.WriteLine(response);
            }
            catch {

            }
            finally {

                await Task.Delay(2000);
            }
        }
    }

    // Need class to handle certificate auth
}