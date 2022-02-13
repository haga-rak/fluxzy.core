using System.Threading.Tasks;

namespace Echoes.Core
{
    public interface ISecureConnectionUpdater
    {
        Task<bool> Upgrade(IDownStreamConnection downStreamConnection, string host, int port);
    }
}