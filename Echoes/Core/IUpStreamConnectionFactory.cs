using System.Threading.Tasks;

namespace Echoes.Core
{
    internal interface IUpStreamConnectionFactory
    {
        Task<IUpstreamConnection> CreateTunneledConnection(string hostName, int port);

        Task<IUpstreamConnection> CreateServerConnection(string hostName, int port, bool secure, IServerChannelPoolManager poolManager);
    }
}