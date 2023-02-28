using System.IO;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.Ssl.BouncyCastle;

public class BouncyCastleConnectionBuilder : ISslConnectionBuilder
{
    public async Task<SslConnection> AuthenticateAsClient(Stream innerStream, SslClientAuthenticationOptions request, CancellationToken token)
    {
        var client = new FluxzyTlsClient(request.TargetHost!, request.EnabledSslProtocols,
            request.ApplicationProtocols!.ToArray());

        var memoryStream = new MemoryStream();
        var nssWriter = new NssLogWriter(memoryStream);
        var protocol = new FluxzyClientProtocol(innerStream, nssWriter);

        // add SNI here 

        if (!string.IsNullOrEmpty(request.TargetHost)) {

        }
        
        protocol.Connect(client);

        var keyInfos = 
            Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int) memoryStream.Position);

        // TODO : Keyinfos may be get updated during runtime, must be updated in SslConnection

        var connection = new SslConnection(protocol.Stream, new SslInfo(protocol), protocol.GetApplicationProtocol())
        {
            NssKey = keyInfos
        };

        return connection; 
    }
}