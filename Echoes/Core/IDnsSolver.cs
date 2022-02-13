using System.Net;
using System.Threading.Tasks;

namespace Echoes.Core
{
    public interface IDnsSolver
    {
        Task<IPAddress> SolveDns(string hostName);
    }
}