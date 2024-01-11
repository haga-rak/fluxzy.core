using Fluxzy.Clients.Ssl.BouncyCastle;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Org.BouncyCastle.X509;
using System.Formats.Asn1;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using CertificateRequest = Org.BouncyCastle.Tls.CertificateRequest;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;
using Cer = Org.BouncyCastle.Tls.Certificate;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Cms;

namespace Fluxzy.Exec
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            throw new InvalidOperationException();

            //var url = "https://api.bddf.staging.bnpparibas/testcertificat"; 

            //var tcpClient = new TcpClient();

            //await tcpClient.ConnectAsync("api.bddf.staging.bnpparibas", 443);

            //using var networkStream = tcpClient.GetStream();

            //var crypto = new FluxzyCrypto();

            //var clientAuth = new ClientAuthentication(crypto); 

            //var client = new FluxzyTlsClient(
            //    "api.bddf.staging.bnpparibas",
            //    SslProtocols.Tls12 | SslProtocols.Tls13,
            //new[] { SslApplicationProtocol.Http11 },
            //        clientAuth, crypto);


            //if (File.Exists("sslkeylog.txt"))
            //    File.Delete("sslkeylog.txt");

            //var nssWriter = new NssLogWriter("sslkeylog.txt"); 

            //var protocol = new FluxzyClientProtocol(networkStream, nssWriter);


            //await protocol.ConnectAsync(client);

            //var fullHeaderGet = "GET https://api.bddf.staging.bnpparibas/testcertificat HTTP/1.1\r\n" + 
            //                    "Host: api.bddf.staging.bnpparibas\r\n" +
            //                    "Accept-Language: fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7\r\n" +
            //                    "Connection: close\r\n" +
            //                    "\r\n";

            //await protocol.Stream.WriteAsync(Encoding.UTF8.GetBytes(fullHeaderGet));

            //StreamReader reader = new StreamReader(protocol.Stream, Encoding.UTF8);

            //try {
            //    while (await reader.ReadLineAsync() is { } line) {

            //        Console.WriteLine(line);

            //        if (line == "\r\n")
            //            break;
            //    }
            //}
            //catch {

            //}

            //Console.WriteLine("Terminé");
            //Console.ReadLine();
        }
    }

    //internal class ClientAuthentication : TlsAuthentication
    //{
    //    private readonly FluxzyCrypto _tlsCrypto;
    //    private readonly X509Certificate2 _certificate;
    //    private string _fileName;
    //    private string _multipass85;

    //    public ClientAuthentication(FluxzyCrypto tlsCrypto)
    //    {
    //        _tlsCrypto = tlsCrypto;

    //        _fileName = @"e:\dump\a.pfx";

    //        _multipass85 = "7eBum@iL";

    //        _certificate =
    //            new X509Certificate2(
    //                _fileName,
    //                _multipass85);
    //    }

    //    public IDictionary<int, byte[]> ClientExtensions { get; set; }

    //    public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
    //    {
    //    }

    //    public TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
    //    {
    //        Pkcs12Store store = new Pkcs12StoreBuilder().Build();

    //        using var stream = File.OpenRead(_fileName);
    //        store.Load(stream, _multipass85.ToCharArray());
    //        var mainCert = store.Aliases.First(); 

    //        var certificateEntry = store.GetCertificate(mainCert);

    //        X509Certificate? x509Certificate = certificateEntry.Certificate;
    //        var tlsCertificate = new BcTlsCertificate(_tlsCrypto, x509Certificate.CertificateStructure);

    //        var certificate = new Certificate(new TlsCertificate[] { tlsCertificate });

    //        var keyEntry = store.GetKey(mainCert);

    //        RsaKeyParameters? rsaKeyParameters = (RsaKeyParameters) keyEntry.Key; //

    //        var tlsCryptoParameters = new TlsCryptoParameters(_tlsCrypto.Context);
            
    //        var signatureAndHashAlgorithm = TlsUtilities.ChooseSignatureAndHashAlgorithm(_tlsCrypto.Context,
    //            certificateRequest.SupportedSignatureAlgorithms, SignatureAlgorithm.rsa);

    //        var credentials = new BcDefaultTlsCredentialedSigner(tlsCryptoParameters,
    //            _tlsCrypto, rsaKeyParameters, certificate,
    //            signatureAndHashAlgorithm);
            
    //        return credentials;
    //    }
    //}
}
