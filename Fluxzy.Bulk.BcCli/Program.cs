using System.Collections;
using System.Net;
using System.Text;
using Fluxzy.Interop.Pcap;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Fluxzy.Bulk.BcCli
{
    /// <summary>
    /// Check list ALPN
    /// Client Certificate (OK) 
    /// Master KEY NOn (OK pour client random et server random )
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await using var tcpProvider = new CapturedTcpConnectionProvider();

            var uriRaw
                = "https://sandbox.smartizy.com/global-health-check";
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

            Stream stream = connection.GetStream();

            var secureRandom = new SecureRandom();

            var cl = new MyTlsClient(new MyCrypto(secureRandom));
            
            var protocol = new TlsClientProtocol(stream) {
                
            };
            

            protocol.Connect(cl);

            stream = protocol.Stream; 

            await stream.WriteAsync(Encoding.ASCII.GetBytes(entireRequest));
            await stream.FlushAsync();

            var streamReader = new StreamReader(stream);

            var response = await streamReader.ReadToEndAsync();

            
            Console.WriteLine(response);
            
            await Task.Delay(2000);
        }
    }

    class MyTlsClient : DefaultTlsClient
    {
        public MyTlsClient(TlsCrypto crypto)
            : base(crypto)
        {
        }

        public override TlsAuthentication GetAuthentication()
        {
            return new MyTlsAuthentication(); 
        }
    }
    

    // Need class to handle certificate auth
    class MyTlsAuthentication : TlsAuthentication
    {
        public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
        {
            
        }

        

        public TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
        {
            return null; 
        }
    }

    class MyCrypto : BcTlsCrypto
    {
        public MyCrypto(SecureRandom sr)
            : base(sr)
        {

        }
        
        public override TlsSecret AdoptSecret(TlsSecret secret)
        {
            return base.AdoptSecret(secret);
        }
        
    }
}