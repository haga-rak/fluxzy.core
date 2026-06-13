// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Default resolver. Looks the process up in the local OS TCP table by connection source port.
    /// Only loopback connections are resolvable this way.
    /// </summary>
    public sealed class LocalTcpTableProcessResolver : IProcessInfoResolver
    {
        private readonly IProcessTracker _processTracker;

        public LocalTcpTableProcessResolver(IProcessTracker? processTracker = null)
        {
            _processTracker = processTracker ?? ProcessTracker.Instance;
        }

        public ValueTask<ProcessInfo?> ResolveAsync(ProcessResolutionContext context, CancellationToken token)
        {
            if (!IPAddress.IsLoopback(context.RemoteEndPoint.Address))
                return new ValueTask<ProcessInfo?>((ProcessInfo?) null);

            return new ValueTask<ProcessInfo?>(_processTracker.GetProcessInfo(context.RemoteEndPoint.Port));
        }
    }
}
