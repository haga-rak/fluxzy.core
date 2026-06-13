// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;

namespace Fluxzy.Utils.NativeOps.SystemProxySetup.macOs
{
    /// <summary>
    /// macOS proxy values that are otherwise hardcoded, each overridable through an environment variable.
    /// Values are read once at startup.
    /// </summary>
    internal static class MacOsProxyConfiguration
    {
        /// <summary>
        /// Path or name of the networksetup binary (FLUXZY_MACOS_NETWORKSETUP_PATH),
        /// e.g. <c>/usr/sbin/networksetup</c> or a custom wrapper. Defaults to <c>networksetup</c>.
        /// </summary>
        public static string NetworkSetupCommand { get; } =
            GetNonEmpty("FLUXZY_MACOS_NETWORKSETUP_PATH") ?? "networksetup";

        /// <summary>
        /// Network services preferred when deciding which one represents the active system proxy
        /// (FLUXZY_MACOS_PROXY_PRIORITY_SERVICES, comma separated). Defaults to Wi-Fi then Ethernet.
        /// </summary>
        public static IReadOnlyCollection<string> PriorityServices { get; } =
            ParseList(Environment.GetEnvironmentVariable("FLUXZY_MACOS_PROXY_PRIORITY_SERVICES"))
            ?? new[] { "Wi-Fi", "Ethernet" };

        /// <summary>
        /// When set, restricts proxy configuration to these services (FLUXZY_MACOS_PROXY_SERVICES,
        /// comma separated); otherwise every active service is configured.
        /// </summary>
        public static IReadOnlyCollection<string>? ServiceAllowList { get; } =
            ParseList(Environment.GetEnvironmentVariable("FLUXZY_MACOS_PROXY_SERVICES"));

        public static IReadOnlyCollection<string>? ParseList(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var items = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return items.Length == 0 ? null : items;
        }

        private static string? GetNonEmpty(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
