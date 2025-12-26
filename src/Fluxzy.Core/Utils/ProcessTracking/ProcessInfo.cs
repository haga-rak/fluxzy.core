// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Contains basic information about a process.
    /// </summary>
    public sealed class ProcessInfo
    {
        public ProcessInfo(int processId, string? processPath)
        {
            ProcessId = processId;
            ProcessPath = processPath;
        }

        /// <summary>
        /// The process identifier (PID).
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// The full path to the process executable, or null if it cannot be determined.
        /// </summary>
        public string? ProcessPath { get; }
    }
}
