// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Core;

namespace Fluxzy.Clients.Dns
{
    internal class DefaultDnsSolver : IDnsSolver
    {
        private readonly ConcurrentDictionary<string, IPAddress> _cache = new();

        public async Task<IPAddress> SolveDns(string hostName)
        {
            if (_cache.TryGetValue(hostName, out var cached))
                return cached;

            try {
                var entry = await System.Net.Dns.GetHostAddressesAsync(hostName);

                return _cache[hostName] = entry.OrderBy(a => a.AddressFamily == AddressFamily.InterNetworkV6)
                                               .First();
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
                return await SolveDns(hostName);
            }
            catch {
                // it's quiet solving 
                return null;
            }
        }
    }
}
