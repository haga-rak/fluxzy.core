using System.Net;
using System.Reflection.Metadata;
using System.Security.Authentication;
using System.Text;
using Fluxzy.Interop.Pcap;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Org.BouncyCastle.Utilities.Encoders;

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
            await using var tcpProvider = new CapturedTcpConnectionProvider();
            
            var uriRaw
                = "https://www.google.com/";
            if (!Uri.TryCreate(uriRaw, UriKind.Absolute, out var uri)) {
                throw new Exception("Invalid URI");
            }

            var connection = tcpProvider.Create("test.pcap");

            var ipAdress = (await Dns.GetHostAddressesAsync(uri.Host)).First();

            var endPoint = await connection.ConnectAsync(ipAdress, uri.Port);

            var entireRequest = $"GET {uri.PathAndQuery} HTTP/1.1\r\n" +
                                $"Host: {uri.Host}\r\n" +
                                $"User-Agent: Coco\r\n" +
                                $"Accept: text/plain\r\n" +
                                $"Connection: close\r\n" +
                                $"X-Header-Popo: Dodo\r\n\r\n";

            var stream = connection.GetStream();
            var secureRandom = new SecureRandom();
            var crypto = new FluxzyCrypto(secureRandom);

            using var nssWriter = new NssLogWriter("ssl.txt");
            
            var cl = new FluxzyTlsClient(crypto, SslProtocols.Tls12);

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
    class FluxzyTlsAuthentication : TlsAuthentication
    {
        public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
        {
            
        }

        public TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
        {
            
            
            return null; 
        }
    }
}