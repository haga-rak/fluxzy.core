using System.IO;
using System.Net.Security;
using System.Threading.Tasks;

namespace Echoes.Core
{
    public interface ISecureConnectionUpdater
    {
        Task<SslStream> AuthenticateAsServer(Stream stream, string host);
    }
}