using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Echoes.Core
{
    internal class DefaultDnsSolver : IDnsSolver
    {
        public async Task<IPAddress> SolveDns(string hostName)
        {
            var entry = await Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false); 
            return entry.First();
        }
    }
}