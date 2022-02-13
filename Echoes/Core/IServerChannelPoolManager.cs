using System;
using System.Threading.Tasks;

namespace Echoes.Core
{
    /// <summary>
    /// Manage connectivity with remote HOSTS
    /// </summary>
    public interface IServerChannelPoolManager : IDisposable
    {
        Task AnticipateSecureConnectionCreation(string hostName, int port);

        Task<IUpstreamConnection> CreateTunneledConnection(string hostName, int port);

        Task<IUpstreamConnection> GetRemoteConnection(string hostName, int port, bool secure);

        Task Return(IUpstreamConnection upstreamConnection, bool close);

        bool IsNotRevoked(IUpstreamConnection upstreamConnection);
    }


}