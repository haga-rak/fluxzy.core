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

            RedactSettingsInArchive =
                Environment.GetEnvironmentVariable("FLUXZY_REDACT_SETTINGS_IN_ARCHIVE") == "1";

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
        ///     Buffer size used when copying non-chunked response bodies in HTTP/1.1 downstream writes.
        /// </summary>
        public static int ResponseBodyCopyBuffer { get; set; } =
            EnvironmentUtility.GetInt32("FLUXZY_RESPONSE_BODY_COPY_BUFFER", 64 * 1024);

        /// <summary>
        ///     Cap applied to HTTP/2 upload DATA frame payloads, even when the remote peer
        ///     advertises a larger SETTINGS_MAX_FRAME_SIZE. Also sizes the pooled buffer rented
        ///     for each request body upload. Values below 16 KiB reduce upload throughput.
        ///     Can be set by environment variable FLUXZY_H2_MAX_UPLOAD_PAYLOAD_SIZE.
        /// </summary>
        public static int H2MaxUploadPayloadSize { get; set; } =
            EnvironmentUtility.GetInt32("FLUXZY_H2_MAX_UPLOAD_PAYLOAD_SIZE", 64 * 1024);

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
        ///     When set to true, the FluxzySetting snapshot embedded in archive meta information will
        ///     omit alteration rules and the save filter (in addition to the always-on redaction of
        ///     credentials and local file paths). Settable via the FLUXZY_REDACT_SETTINGS_IN_ARCHIVE
        ///     environment variable.
        /// </summary>
        public static bool RedactSettingsInArchive { get; set; }

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
        ///     When true, raw capture also listens on the loopback interface so connections
        ///     to local upstreams are recorded. Disabled by default.
        ///     Can be set by environment variable FLUXZY_CAPTURE_LOOPBACK=1.
        /// </summary>
        public static bool CaptureLoopbackTraffic { get; set; } =
            Environment.GetEnvironmentVariable("FLUXZY_CAPTURE_LOOPBACK") == "1";

        /// <summary>
        ///     The cache validity duration in seconds for process tracking information.
        ///     Can be set by environment variable FLUXZY_PROCESS_TRACKER_CACHE_SECONDS.
        /// </summary>
        public static int ProcessTrackerCacheSeconds { get; set; } =
            EnvironmentUtility.GetInt32("FLUXZY_PROCESS_TRACKER_CACHE_SECONDS", 30);

        /// <summary>
        ///     When true, Fluxzy never decodes brotli bodies and removes "br" from the accept-encoding
        ///     header of forwarded requests so that upstream servers fall back to gzip or deflate.
        ///     Defaults to true when the current .NET runtime ships a Brotli decoder affected by
        ///     CVE-2026-111497 (before 8.0.33, 9.0.22, 10.0.14 and 11.0.0-rc.3).
        ///     The FLUXZY_DISABLE_BROTLI environment variable overrides the default with "1" or "0".
        /// </summary>
        public static bool DisableBrotli { get; set; } = ComputeDisableBrotli(out BrotliAutoDisabled);

        internal static readonly bool BrotliAutoDisabled;

        internal static string BrotliAutoDisabledMessage =>
            $"Brotli decoding disabled: .NET runtime {Environment.Version} ships a Brotli decoder affected by " +
            "CVE-2026-111497. Update the runtime (8.0.33, 9.0.22 or 10.0.14) or set FLUXZY_DISABLE_BROTLI=0 " +
            "to re-enable it. Impersonation profiles will advertise accept-encoding without br.";

        private static bool ComputeDisableBrotli(out bool autoDisabled)
        {
            autoDisabled = false;

            switch (Environment.GetEnvironmentVariable("FLUXZY_DISABLE_BROTLI")?.Trim()) {
                case "1":
                    return true;
                case "0":
                    return false;
            }

            autoDisabled = IsBrotliRuntimeVulnerable(Environment.Version);

            return autoDisabled;
        }

        internal static bool IsBrotliRuntimeVulnerable(Version runtime)
        {
            var fixedBuild = runtime.Major switch {
                < 8 => int.MaxValue,
                8 => 33,
                9 => 22,
                10 => 14,
                11 => 0,
                _ => -1
            };

            if (fixedBuild < 0)
                return false;

            if (runtime.Major == 11)
                return IsPreviewBefore(runtime, "rc.3");

            return runtime.Build < fixedBuild;
        }

        private static bool IsPreviewBefore(Version runtime, string fixedPreRelease)
        {
            var informational = typeof(object).Assembly
                                              .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                              ?.InformationalVersion ?? string.Empty;

            if (runtime.Build > 0)
                return false;

            var dash = informational.IndexOf('-');

            if (dash < 0)
                return false;

            var preRelease = informational.Substring(dash + 1).Split('+')[0];

            return string.CompareOrdinal(preRelease, fixedPreRelease) < 0;
        }

        /// <summary>
        ///     True when Fluxzy runs under Fluxzy Desktop (detected via the FluxzyDesktopVersion
        ///     environment variable). Gates desktop-only hot-path checks like live-edit header scanning.
        /// </summary>
        public static bool LiveEditEnabled { get; } =
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FluxzyDesktopVersion"));
    }
}
