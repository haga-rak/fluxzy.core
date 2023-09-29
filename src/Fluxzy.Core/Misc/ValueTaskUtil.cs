// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    public static class IpUtility
    {
        public static HashSet<IPAddress> LocalAddresses { get; } = GetAllLocalIp();

        internal static HashSet<IPAddress> GetAllLocalIp()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                                   .Where(x => 
                                               x.OperationalStatus == OperationalStatus.Up)
                                   .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                                   .Where(x =>
                                       x.Address.AddressFamily == AddressFamily.InterNetwork
                                       || x.Address.AddressFamily == AddressFamily.InterNetworkV6)
                                   .Select(x => x.Address)
                                   .ToHashSet();
        }
    }

    public static class ValueTaskUtil
    {
        public static async ValueTask<T[]> WhenAll<T>(IList<ValueTask<T>> tasks)
        {
            if (tasks.Count == 0)
                return Array.Empty<T>();

            List<Exception>? exceptions = null;

            var results = new T[tasks.Count];

            for (var i = 0; i < tasks.Count; i++) {
                try {
                    results[i] = await tasks[i].ConfigureAwait(false);
                }
                catch (Exception ex) {
                    exceptions ??= new List<Exception>(tasks.Count);
                    exceptions.Add(ex);
                }
            }

            return exceptions is null
                ? results
                : throw new AggregateException(exceptions);
        }
    }
}
