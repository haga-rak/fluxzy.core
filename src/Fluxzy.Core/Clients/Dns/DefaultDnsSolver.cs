// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;

namespace Fluxzy.Clients.Dns
{
    internal class DefaultDnsSolver : IDnsSolver
    {
        private readonly ConcurrentDictionary<string, IPAddress> _cache = new();

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreRepository =
            new(StringComparer.OrdinalIgnoreCase);

        public async Task<IPAddress> SolveDns(string hostName, ProxyConfiguration? proxyConfiguration)
        {
            var lockKey = _semaphoreRepository.GetOrAdd(hostName, new SemaphoreSlim(1));

            try {
                await lockKey.WaitAsync().ConfigureAwait(false);

                if (_cache.TryGetValue(hostName, out var cached))
                    return cached;

                try {
                    var result = await InternalSolveDns(hostName, proxyConfiguration);

                    return _cache[hostName] = result;
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
            finally {
                lockKey.Release();

                if (_semaphoreRepository.TryGetValue(hostName, out var sem) && sem.CurrentCount == 0)
                    _semaphoreRepository.TryRemove(hostName, out _);
            }

        }

        protected virtual async Task<IPAddress> InternalSolveDns(string hostName, ProxyConfiguration? proxyConfiguration)
        {
            var entry = await System.Net.Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false);

            var result = entry.OrderBy(a => a.AddressFamily == AddressFamily.InterNetworkV6)
                              .First();

            return result;
        }

        public async Task<IPAddress?> SolveDnsQuietly(string hostName, ProxyConfiguration? proxyConfiguration)
        {
            try {
                return await SolveDns(hostName, proxyConfiguration).ConfigureAwait(false);
            }
            catch {
                // it's quiet solving 
                return null;
            }
        }
    }
}
