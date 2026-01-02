// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Reflection;
using Fluxzy.Utils;

namespace Fluxzy
{
    /// <summary>
    ///     Contains static low level settings for Fluxzy
    /// </summary>
    public static class FluxzySharedSetting
    {
        static FluxzySharedSetting()
        {
            var rawValue = Environment.GetEnvironmentVariable("OverallMaxConcurrentConnections");

            if (rawValue != null && int.TryParse(rawValue, out var value) && value > 0) {
                OverallMaxConcurrentConnections = value;
            }

            SkipCollectingEnvironmentInformation =
                Environment.GetEnvironmentVariable("SkipCollectingEnvironmentInformation") == "1";

            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("appdata"))) {
                // for macOS and linux, this environment variable used in several temp file (certcache) is not 
                // set leading unwanted folder creation

                Environment.SetEnvironmentVariable("appdata",
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            }
        }

        /// <summary>
        ///     The buffer size used to process this request
        /// </summary>
        public static int RequestProcessingBuffer { get; set; } = 1024 * 4;

        /// <summary>
        ///     The maximum number of concurrent connections allowed
        /// </summary>
        public static int OverallMaxConcurrentConnections { get; } = 102400;

        /// <summary>
        ///     If true, the proxy will use HTTP 528 to inform remote connection error,
        ///     otherwise it will use HTTP 502
        /// </summary>
        public static bool Use528 { get; set; } = true;

        /// <summary>
        ///     When set to true, the proxy will not collect environment information on the archive file
        /// </summary>
        public static bool SkipCollectingEnvironmentInformation { get; set; }

        /// <summary>
        /// </summary>
        public static int DownStreamProviderReceiveTimeoutMilliseconds { get; } =
            EnvironmentUtility.GetInt32("FLUXZY_DOWNSTREAM_CONNECTION_RECEIVE_TIMEOUT_MILLISECONDS", -1);

        /// <summary>
        ///     Maximum number of connection per remote host
        /// </summary>
        public static int MaxConnectionPerHost { get; set; } =
            EnvironmentUtility.GetInt32("FLUXZY_MAX_CONNECTION_PER_HOST", 16);

        /// <summary>
        ///     Fluxzy will use stackalloc for buffer allocation if the buffer size is less than this value.
        ///     Can bet set by environment variable FLUXZY_STACK_ALLOC_THRESHOLD
        /// </summary>
        public static int StackAllocThreshold { get; set; } =
            EnvironmentUtility.GetInt32("FLUXZY_STACK_ALLOC_THRESHOLD", 1024);

        /// <summary>
        ///     The running version of Fluxzy
        /// </summary>
        public static string RunningVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version!.ToString();

        /// <summary>
        ///     Number of maximum attempt for proxy authentication before closing the connection
        /// </summary>
        public static int ProxyAuthenticationMaxAttempt { get; set; } =
            EnvironmentUtility.GetInt32("FLUXZY_PROXY_AUTHENTICATION_MAX_ATTEMPT", 3);

        /// <summary>
        ///     The maximum buffer size for request processing, above this value the connection will be abandoned
        /// </summary>
        public static int MaxProcessingBuffer { get; } =
            EnvironmentUtility.GetInt32("FLUXZY_MAX_PROCESSING_BUFFER", 1024 * 512);

        /// <summary>
        ///     Maximum length of an enhanced block in a pcapng file, default is 8KB
        /// </summary>
        public static int PcapEnhancedBlockMaxLength { get; } =
            EnvironmentUtility.GetInt32("FLUXZY_PCAP_ENHANCED_BLOCK_MAX_LENGTH", 8 * 1024);
        
        public static bool NoCacheOnFqdn { get; internal set; } =
            Environment.GetEnvironmentVariable("NoCacheOnFqdnCompute") == "1";

        /// <summary>
        ///     The delay in milliseconds before tearing down a raw capture connection after an explicit close
        /// </summary>
        public static int RawCaptureLingerDelayBeforeTearDownMillis { get; set; } =
            EnvironmentUtility.GetInt32("RAW_CAPTURE_LINGER_DELAY_BEFORE_TEARDOWN", 200);

        /// <summary>
        ///     The cache validity duration in seconds for process tracking information.
        ///     Can be set by environment variable FLUXZY_PROCESS_TRACKER_CACHE_SECONDS.
        /// </summary>
        public static int ProcessTrackerCacheSeconds { get; set; } =
            EnvironmentUtility.GetInt32("FLUXZY_PROCESS_TRACKER_CACHE_SECONDS", 30);
    }
}
