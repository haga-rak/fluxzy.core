// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Resolves the process behind a downstream connection. The default implementation reads the
    /// local OS TCP table. Callers fronting the proxy with a transparent tunnel (for example
    /// tun2socks over SOCKS5) can provide their own to map a connection back to the real process.
    /// </summary>
    public interface IProcessInfoResolver
    {
        /// <summary>
        /// Returns the process owning <paramref name="context"/>, or null when it cannot be determined.
        /// Called once per downstream connection when process tracking is enabled.
        /// </summary>
        ValueTask<ProcessInfo?> ResolveAsync(ProcessResolutionContext context, CancellationToken token);
    }
}
