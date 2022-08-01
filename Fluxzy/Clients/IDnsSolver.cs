using System.Net;
using System.Threading.Tasks;

namespace Fluxzy.Clients
{
    public interface IDnsSolver
    {
        Task<IPAddress> SolveDns(string hostName);
    }
}