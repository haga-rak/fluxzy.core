// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Fluxzy.Core;

namespace Fluxzy.Clients.Dns
{
    internal class DefaultDnsResolver : IDnsSolver
    {
        private readonly ConcurrentDictionary<string, IReadOnlyCollection<IPAddress>> _cache = new();

        private readonly AsyncKeyedLocker<string> _asyncKeyedLocker =
            new(StringComparer.OrdinalIgnoreCase);

        public async Task<IReadOnlyCollection<IPAddress>> SolveDnsAll(string hostName)
        {
            using var _ = await _asyncKeyedLocker.LockAsync(hostName).ConfigureAwait(false);

            if (_cache.TryGetValue(hostName, out var cached))
                return cached;

            try
            {
                var result = await InternalSolveDns(hostName).ConfigureAwait(false);
                return _cache[hostName] = result.ToList();
            }
            catch (Exception ex)
            {
                var errorCode = -1;

                if (ex is SocketException sex)
                    errorCode = sex.ErrorCode;

                var clientErrorException = new ClientErrorException(
                    errorCode, $"Failed to solve DNS for {hostName}", ex.Message);

                throw clientErrorException;
            }
        }

        public async Task<IPAddress> SolveDns(string hostName)
        {
            var all = (await SolveDnsAll(hostName).ConfigureAwait(false));
            var found = all.FirstOrDefault();

            if (found == null)
                throw new ClientErrorException(-1, $"Failed to solve DNS for {hostName}",
                    "No IP address found");

            return found;
        }

        protected virtual async Task<IEnumerable<IPAddress>> InternalSolveDns(string hostName)
        {
            var entry = await System.Net.Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false);
            var result = entry.OrderBy(a => a.AddressFamily == AddressFamily.InterNetworkV6);
            return result;
        }

        public async Task<IPAddress?> SolveDnsQuietly(string hostName)
        {
            try
            {
                return await SolveDns(hostName).ConfigureAwait(false);
            }
            catch
            {
                // it's quiet solving 
                return null;
            }
        }
    }
}
