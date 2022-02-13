using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Core
{
    public interface IConnectionHandler 
    {
        Task Process(IServerChannelPoolManager poolManager, IDownStreamConnection downStreamConnection, CancellationToken token);
    }
}