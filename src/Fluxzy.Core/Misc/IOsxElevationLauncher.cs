// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Diagnostics;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    /// <summary>
    /// Alternative macOS elevation mechanism. When assigned to
    /// <see cref="ProcessUtils.OsxElevationLauncher"/>, it replaces the built-in flow
    /// (system authorization dialog, sudo) entirely.
    /// </summary>
    public interface IOsxElevationLauncher
    {
        /// <summary>
        /// Starts <paramref name="commandName"/> as root. When <paramref name="redirectStdOut"/>
        /// is true, the returned process must expose live stdin/stdout. Returns null when
        /// elevation is declined or unavailable.
        /// </summary>
        Task<Process?> StartElevatedAsync(
            string commandName, string[] args, bool redirectStdOut,
            string askPasswordPrompt, bool redirectStandardError);
    }
}
