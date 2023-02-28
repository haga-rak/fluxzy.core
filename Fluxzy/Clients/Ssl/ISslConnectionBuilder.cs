using System;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Clients.Ssl
{
    public interface ISslConnectionBuilder
    {
        Task<SslConnection> AuthenticateAsClient(Stream innerStream, SslClientAuthenticationOptions request, CancellationToken token);
    }


    public class SslConnection : IDisposable
    {
        public SslConnection(Stream stream, SslInfo sslInfo)
        {
            Stream = stream;
            SslInfo = sslInfo;
        }

        public Stream Stream { get;  }

        public SslInfo SslInfo { get;  }

        public string ? NssKey { get; set; }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
