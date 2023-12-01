// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    /// <summary>
    /// Contains static low level settings for Fluxzy
    /// </summary>
    public static class FluxzySharedSetting
    {
        static FluxzySharedSetting()
        {
            var rawValue = Environment.GetEnvironmentVariable("OverallMaxConcurrentConnections");

            if (rawValue != null && int.TryParse(rawValue, out var value) && value > 0)
                OverallMaxConcurrentConnections = value;

            SkipCollectingEnvironmentInformation = Environment.GetEnvironmentVariable("SkipCollectingEnvironmentInformation") == "1";
        }

        /// <summary>
        /// The buffer size used to process this request
        /// </summary>
        public static int RequestProcessingBuffer { get; set; } = 1024 * 4; 

        /// <summary>
        /// The maximum number of concurrent connections allowed
        /// </summary>
        public static int OverallMaxConcurrentConnections { get;  } = 102400;

        /// <summary>
        ///  If true, the proxy will use HTTP 528 to inform remote connection error,
        ///  otherwise it will use HTTP 502
        /// </summary>
        public static bool Use528 { get; set; } = true; 

        /// <summary>
        /// When set to true, the proxy will not collect environment information on the archive file
        /// </summary>
        public static bool SkipCollectingEnvironmentInformation { get; set; }
    }
}
