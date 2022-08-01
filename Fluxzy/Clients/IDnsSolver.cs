using System.Net;
using System.Threading.Tasks;

namespace Echoes.Clients
{
    public interface IDnsSolver
    {
        Task<IPAddress> SolveDns(string hostName);
    }
}