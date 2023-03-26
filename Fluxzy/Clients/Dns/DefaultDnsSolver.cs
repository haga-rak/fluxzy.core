// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Core;

namespace Fluxzy.Clients.Dns
{
    internal class DefaultDnsSolver : IDnsSolver
    {
        public async Task<IPAddress> SolveDns(string hostName)
        {
            try {
                var entry = await System.Net.Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false);

                return entry.OrderByDescending(a => a.AddressFamily == AddressFamily.InterNetworkV6).First();
            }
            catch (Exception ex) {
                var errorCode = -1;

                if (ex is SocketException sex)
                    errorCode = sex.ErrorCode;

                var clientErrorException = new ClientErrorException(
                    errorCode, $"Failed to solve DNS for {hostName}", ex.Message);

                throw clientErrorException;
            }
        }
        public async Task<IPAddress?> SolveDnsQuietly(string hostName)
        {
            try {
                return await SolveDns(hostName).ConfigureAwait(false); 
            }
            catch {
                // it's quiet solving 
                return null; 
            }
        }
    }
}
