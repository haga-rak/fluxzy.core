// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Misc
{
    /// <summary>
    /// Configuration options for the mDNS proxy discovery service.
    /// </summary>
    public record MdnsAnnouncerOptions
    {
        /// <summary>
        /// The service name to advertise (e.g., "Fluxzy-DESKTOP-ABC123").
        /// </summary>
        public required string ServiceName { get; init; }

        /// <summary>
        /// The proxy port to advertise.
        /// </summary>
        public required int ProxyPort { get; init; }

        /// <summary>
        /// The host IP address to advertise.
        /// </summary>
        public required string HostIpAddress { get; init; }

        /// <summary>
        /// The hostname to advertise. Defaults to the machine name.
        /// </summary>
        public string HostName { get; init; } = Environment.MachineName;

        /// <summary>
        /// The operating system name. Defaults to the current OS version.
        /// </summary>
        public string OsName { get; init; } = Environment.OSVersion.ToString();

        /// <summary>
        /// The Fluxzy version string.
        /// </summary>
        public string FluxzyVersion { get; init; } = "1.0.0";

        /// <summary>
        /// Additional startup settings or configuration to display.
        /// </summary>
        public string FluxzyStartupSetting { get; init; } = "";

        /// <summary>
        /// The endpoint path where clients can fetch the root certificate.
        /// </summary>
        public string CertEndpoint { get; init; } = "/ca";

        /// <summary>
        /// Number of initial announcements to send on startup.
        /// </summary>
        public int InitialAnnouncementCount { get; init; } = 3;

        /// <summary>
        /// Delay between initial announcements in milliseconds.
        /// </summary>
        public int InitialAnnouncementDelayMs { get; init; } = 250;
    }
}
