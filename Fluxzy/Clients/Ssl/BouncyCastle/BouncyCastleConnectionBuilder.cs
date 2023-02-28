using System;
using System.IO;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    public class BouncyCastleConnectionBuilder : ISslConnectionBuilder
    {
        private static readonly object _sslFileLocker = new(); 

        public async Task<SslConnection> AuthenticateAsClient(Stream innerStream, SslClientAuthenticationOptions request, CancellationToken token)
        {
            var client = new FluxzyTlsClient(
                request.TargetHost!, 
                request.EnabledSslProtocols,
                request.ApplicationProtocols!.ToArray());

            var memoryStream = new MemoryStream();
            var nssWriter = new NssLogWriter(memoryStream);
            var protocol = new FluxzyClientProtocol(innerStream, nssWriter);

            await Task.Run(() => protocol.Connect(client), token); // BAD but necessary

            var keyInfos = 
                Encoding.UTF8.GetString(memoryStream.ToArray());

            // TODO : Keyinfos may be get updated during runtime, must be updated in SslConnection
            
            var connection = new SslConnection(protocol.Stream, new SslInfo(protocol), protocol.GetApplicationProtocol())
            {
                NssKey = keyInfos
            };

            if (connection.NssKey != null && Environment.GetEnvironmentVariable("SSLKEYLOGFILE") != null) {
                lock (_sslFileLocker)
                    File.AppendAllText(Environment.GetEnvironmentVariable("SSLKEYLOGFILE"), connection.NssKey);
            }

            return connection; 
        }
    }
}