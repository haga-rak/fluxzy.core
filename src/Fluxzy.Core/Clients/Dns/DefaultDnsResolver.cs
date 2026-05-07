// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Utils;

namespace Fluxzy.Clients.Dns
{
    internal class DefaultDnsResolver : IDnsSolver
    {
        private readonly ConcurrentDictionary<string, IReadOnlyCollection<IPAddress>> _cache = new();

        public async Task<IReadOnlyCollection<IPAddress>> SolveDnsAll(string hostName)
        {
            using var _ = await Synchronizer<string>.Shared.LockAsync(hostName).ConfigureAwait(false);

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
                var networkErrorCode = NetworkErrorCodes.DnsFailure;

                if (ex is SocketException sex) {
                    errorCode = sex.ErrorCode;
                    networkErrorCode = MapDnsSocketError(sex.SocketErrorCode);
                }

                var clientErrorException = new ClientErrorException(
                    errorCode, $"Failed to solve DNS for {hostName}",
                    innerMessageException: ex.Message,
                    networkErrorCode: networkErrorCode);

                throw clientErrorException;
            }

        }

        public async Task<IPAddress> SolveDns(string hostName)
        {
            if (IPAddress.TryParse(hostName, out var immediateValue))
                return immediateValue;

            var all = (await SolveDnsAll(hostName).ConfigureAwait(false));
            var found = all.FirstOrDefault();

            if (found == null)
                throw new ClientErrorException(-1, $"Failed to solve DNS for {hostName}",
                    innerMessageException: "No IP address found",
                    networkErrorCode: NetworkErrorCodes.DnsNoData);

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
            try {
                return await SolveDns(hostName).ConfigureAwait(false);
            }
            catch {
                // it's quiet solving
                return null;
            }
        }

        internal static string MapDnsSocketError(SocketError socketError)
        {
            return socketError switch {
                SocketError.HostNotFound => NetworkErrorCodes.DnsNotFound,
                SocketError.NoData => NetworkErrorCodes.DnsNoData,
                SocketError.TryAgain => NetworkErrorCodes.DnsTryAgain,
                _ => NetworkErrorCodes.DnsFailure
            };
        }
    }
}
