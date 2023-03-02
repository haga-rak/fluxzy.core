using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using Fluxzy.Clients.Ssl.BouncyCastle;
using Fluxzy.Core;
using Fluxzy.Interop.Pcap;
using Fluxzy.Interop.Pcap.Cli.Clients;
using Fluxzy.Interop.Pcap.Pcapng;
using Fluxzy.Interop.Pcap.Pcapng.Structs;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using YamlDotNet.Serialization;

namespace Fluxzy.Bulk.BcCli
{
    /// <summary>
    /// Check list ALPN
    /// Client Certificate (OK) 
    /// Master KEY NOn (OK pour client random et server random )
    /// </summary>
    internal class Program
    {
        static void Main()
        {
            using var fileStream = File.Create("test.pcapng");
            var writer = new PcapngWriter("fluxzy - https://www.fluxzy.io");


            writer.WriteHeader(fileStream);
        }

        private static async Task QuickCaptureWithBouncy()
        {
            var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                (a) => new OutOfProcessCaptureContext(a));

            await using var tcpProvider = await CapturedTcpConnectionProvider.Create(scope, false);

            var uriRaw
                = "https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ";
            // = "https://extranet.2befficient.fr/ip";
            //= "https://sandbox.smartizy.com/ip";
            if (!Uri.TryCreate(uriRaw, UriKind.Absolute, out var uri)) {
                throw new Exception("Invalid URI");
            }

            var connection = tcpProvider.Create("test.pcap");

            var ipAddress = (await Dns.GetHostAddressesAsync(uri.Host)).First();

            var endPoint = await connection.ConnectAsync(ipAddress, uri.Port);

            //ProtocolName.Http_2_Tls

            var entireRequest = $"GET {uri.PathAndQuery} HTTP/1.1\r\n" +
                                $"Host: {uri.Host}\r\n" +
                                $"User-Agent: Coco\r\n" +
                                $"Accept: text/plain\r\n" +
                                $"Connection: close\r\n" +
                                $"X-Header-Popo: Dodo\r\n\r\n";

            var stream = connection.GetStream();

            using var nssWriter = new NssLogWriter("ssl.txt");

            var fluxzyTlsClient = new FluxzyTlsClient(uri.Host, SslProtocols.Tls12 | SslProtocols.Tls11,
                new[] {SslApplicationProtocol.Http11,});

            var protocol = new FluxzyClientProtocol(stream, nssWriter);

            protocol.Connect(fluxzyTlsClient);

            var sessionParameters = protocol.SessionParameters;
            var cipherSuite = (TlsCipherSuite) sessionParameters.CipherSuite;
            var str = protocol.ApplicationProtocol.GetUtf8Decoding();

            var listOfCertificate = new List<TlsCertificate>();

            for (int i = 0; i < protocol.SessionParameters.PeerCertificate.Length; i++) {
                var cert = (BcTlsCertificate) protocol.SessionParameters.PeerCertificate.GetCertificateAt(i);
                listOfCertificate.Add(cert);

                var encodable = cert.GetSigAlgParams();
                var issuer = cert.X509CertificateStructure.Issuer.ToString();
                var subject = cert.X509CertificateStructure.Subject.ToString();
            }

            stream = protocol.Stream;

            await stream.WriteAsync(Encoding.ASCII.GetBytes(entireRequest));
            await stream.FlushAsync();

            var streamReader = new StreamReader(stream);

            try {
                var response = await streamReader.ReadToEndAsync();

                Console.WriteLine(response);
            }
            catch (Exception ex) {
            }
            finally {
                await Task.Delay(2000);
            }
        }
    }

    // Need class to handle certificate auth
}