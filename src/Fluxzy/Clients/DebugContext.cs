// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;

namespace Fluxzy.Clients
{
    public static class DebugContext
    {
        static DebugContext()
        {
            var fileDump = Environment
                           .GetEnvironmentVariable("Fluxzy_EnableNetworkFileDump")?.Trim();

            EnableNetworkFileDump = string.Equals(fileDump, "true", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(fileDump, "1", StringComparison.OrdinalIgnoreCase);

            var windowSizeTrace = Environment
                                  .GetEnvironmentVariable("Fluxzy_EnableWindowSizeTrace")?.Trim();

            EnableWindowSizeTrace = string.Equals(windowSizeTrace, "true", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(windowSizeTrace, "1", StringComparison.OrdinalIgnoreCase);

            NetworkFileDumpDirectory = Environment
                                       .GetEnvironmentVariable("Fluxzy_FileDumpDirectory")?.Trim() ?? "raw";

            if (EnableNetworkFileDump)
                Directory.CreateDirectory(NetworkFileDumpDirectory);

            if (EnableWindowSizeTrace)
                Directory.CreateDirectory(WindowSizeTraceDumpDirectory);
        }

        /// <summary>
        ///     Reference for current debug sessions
        /// </summary>
        public static string ReferenceString { get; } = DateTime.Now.ToString("yyyyMMddHHmmss");

        /// <summary>
        ///     Get the value whether network file dump is active. Can be modified by setting environment variable
        ///     "Fluxzy_EnableNetworkFileDump"
        /// </summary>
        public static bool EnableNetworkFileDump { get; }

        /// <summary>
        ///     Enable trace on H2 window updates
        /// </summary>
        public static bool EnableWindowSizeTrace { get; }

        public static bool EnableDumpStackTraceOn502 { get; }
            = Environment.GetEnvironmentVariable("EnableDumpStackTraceOn502") == "true";

        public static bool InsertFluxzyMetricsOnResponseHeader { get; }
            = !string.IsNullOrWhiteSpace(Environment
                .GetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader"));

        public static bool IsH2TracingEnabled =>
            Environment.GetEnvironmentVariable("EnableH2Tracing") == "true";

        /// <summary>
        ///     When EnableNetworkFileDump is enable. Get the dump directory. Default value is "./raw".
        ///     Can be modified by setting environment variable "Fluxzy_FileDumpDirectory" ;
        /// </summary>
        public static string NetworkFileDumpDirectory { get; }

        /// <summary>
        ///     When EnableWindowSizeTrace is enabled, store the logs on this directory
        /// </summary>
        public static string WindowSizeTraceDumpDirectory { get; } = "trace";
    }
}
