using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Core;

namespace Fluxzy.Clients.Common
{
    internal class DefaultDnsSolver : IDnsSolver
    {
        public async Task<IPAddress> SolveDns(string hostName)
        {
            try {
                var entry = await Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false);
                return entry.OrderByDescending(a => a.AddressFamily == AddressFamily.InterNetworkV6).First();
            }
            catch (Exception ex) {
                var errorCode = -1; 

                if (ex is SocketException sex) {
                    errorCode = sex.ErrorCode; 
                }

                var clientErrorException = new ClientErrorException(
                    errorCode, $"Failed to solve DNS for {hostName}", ex.Message);

                throw clientErrorException;
            }
        }
    }
}