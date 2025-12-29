// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using MessagePack;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Contains basic information about a process.
    /// </summary>
    [MessagePackObject]
    public sealed class ProcessInfo
    {
        [JsonConstructor]
        [SerializationConstructor]
        public ProcessInfo(int processId, string? processPath)
        {
            ProcessId = processId;
            ProcessPath = processPath;
        }

        /// <summary>
        /// The process identifier (PID).
        /// </summary>
        [Key(0)]
        public int ProcessId { get; }

        /// <summary>
        /// The full path to the process executable, or null if it cannot be determined.
        /// </summary>
        [Key(1)]
        public string? ProcessPath { get; }
    }
}
