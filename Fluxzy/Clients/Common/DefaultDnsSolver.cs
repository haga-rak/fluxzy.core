﻿using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Fluxzy.Clients.Common
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