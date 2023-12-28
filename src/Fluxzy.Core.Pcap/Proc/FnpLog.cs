// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Proc
{
    /// <summary>
    /// A very trivial logger that can be activated through environment variables
    /// </summary>
    internal static class FnpLog
    {
        private static readonly object Lock = new();

        static FnpLog()
        {
            if (LoggingEnabled) {
                var directoryInfo = new FileInfo(LogPath).Directory;
                if (directoryInfo != null && !directoryInfo.Exists) {
                    directoryInfo.Create();
                }
            }
        }

        public static string LogPath { get; } = 
            Environment.GetEnvironmentVariable("FLUXZYNETCAP_LOGGING_PATH") ??
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                 ".fluxzy", "logs", "fluxzynetcap", "log.txt");

        public static bool LoggingEnabled { get; } = 
            Environment.GetEnvironmentVariable("FLUXZYNETCAP_LOGGING_ENABLED") == "1";

        /// <summary>
        /// Log a raw line 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static void Log(string content)
        {
            lock (Lock) {
                var instant = DateTime.Now; 
                File.AppendAllText(LogPath, $"[{instant:s}] {content}\r\n");
            }
        }
    }
}
