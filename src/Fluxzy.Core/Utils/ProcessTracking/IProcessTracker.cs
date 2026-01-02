// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Provides functionality to retrieve process information from a local port number.
    /// </summary>
    public interface IProcessTracker
    {
        /// <summary>
        /// Retrieves process information for the process bound to the specified local port.
        /// </summary>
        /// <param name="localPort">The local port number to look up.</param>
        /// <returns>Process information if found; otherwise, null.</returns>
        ProcessInfo? GetProcessInfo(int localPort);
    }
}
