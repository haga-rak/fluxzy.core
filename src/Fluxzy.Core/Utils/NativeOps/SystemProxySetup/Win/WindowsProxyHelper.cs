// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Runtime.InteropServices;
using Fluxzy.Core.Proxy;

#if NET6_0_OR_GREATER
using Microsoft.Win32;
#endif

namespace Fluxzy.Utils.NativeOps.SystemProxySetup.Win
{
    internal static class WindowsProxyHelper
    {
        private const int InternetOptionSettingsChanged = 39;
        private const int InternetOptionRefresh = 37;

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(
            IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        internal static SystemProxySetting GetSetting()
        {
#if NET6_0_OR_GREATER
            using var registry =
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
                    true);

            if (registry == null)
                throw new InvalidOperationException("Unable to access system registry");

            var proxyEnabled = (int) registry.GetValue("ProxyEnable", 0)! == 1;
            var proxyOverride = (string) registry.GetValue("ProxyOverride", string.Empty)!;
            var proxyServer = (string) registry.GetValue("ProxyServer", string.Empty)!;

            var proxyOverrideList = proxyOverride.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

            var lossless = TryParseProxyServer(proxyServer, out var proxyServerName, out var proxyPort);

            var setting = new SystemProxySetting(proxyServerName, proxyPort, proxyOverrideList) {
                Enabled = proxyEnabled
            };

            // Keep the raw value when it cannot be regenerated from host/port
            // (ftp=, socks=, differing http/https targets) so restore is verbatim
            if (!lossless && !string.IsNullOrEmpty(proxyServer))
                setting.PrivateValues[ProxyConstants.WinProxyServerRawKey] = proxyServer;

            return setting;

#else
            throw new NotSupportedException("This method is only supported on .NET 6.0 or greater");
#endif


        }

        internal static void SetProxySetting(SystemProxySetting systemProxySetting)
        {
#if NET6_0_OR_GREATER
            using var registry =
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
                    true);

            if (registry == null)
                throw new InvalidOperationException("Unable to access system registry");

            registry.SetValue("ProxyEnable", systemProxySetting.Enabled ? 1 : 0);

            // If the setting carries a preserved raw ProxyServer string (round-trip from
            // a previous GetSetting() call that could not parse it as "host:port"), write
            // it back verbatim so corporate/legacy configurations are not corrupted.
            if (systemProxySetting.PrivateValues.TryGetValue(ProxyConstants.WinProxyServerRawKey, out var rawObj)
                && rawObj is string rawProxyServer
                && !string.IsNullOrEmpty(rawProxyServer)) {
                registry.SetValue("ProxyServer", rawProxyServer);
            }
            else if (systemProxySetting.BoundHost == null
                || systemProxySetting.BoundHost == ProxyConstants.NoProxyWord) {
                // Remove proxy setting
                registry.DeleteValue("ProxyServer", false);
            }
            else {
                registry.SetValue("ProxyServer",
                    BuildProxyServerValue(systemProxySetting.BoundHost, systemProxySetting.ListenPort));
            }

            var proxyOverrideLine = string.Join(";", systemProxySetting.ByPassHosts);
            registry.SetValue("ProxyOverride", proxyOverrideLine);

            InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
#else
            throw new NotSupportedException("This method is only supported on .NET 6.0 or greater");
#endif
        }
        /// <summary>
        /// Per-protocol form so that WinINET only routes http and https through Fluxzy.
        /// A bare "host:port" applies to every protocol, including ftp (#821).
        /// </summary>
        internal static string BuildProxyServerValue(string host, int port)
        {
            return $"http={host}:{port};https={host}:{port}";
        }

        /// <summary>
        /// Accepts "host:port" and "scheme=host:port;..." lists. Returns true when the
        /// value can be regenerated from host/port without loss.
        /// </summary>
        internal static bool TryParseProxyServer(string? proxyServer, out string host, out int port)
        {
            host = ProxyConstants.NoProxyWord;
            port = -1;

            if (string.IsNullOrWhiteSpace(proxyServer))
                return false;

            if (!proxyServer!.Contains('=')) {
                if (!TryParseHostPort(proxyServer, out host, out port))
                    host = ProxyConstants.NoProxyWord;

                // A bare host:port covers every protocol; the per-protocol value
                // Fluxzy writes cannot reproduce it, so keep the raw string for restore
                return false;
            }

            string? httpTarget = null;
            string? httpsTarget = null;
            var otherEntries = false;

            foreach (var entry in proxyServer.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
                var separator = entry.IndexOf('=');

                if (separator <= 0) {
                    otherEntries = true;
                    continue;
                }

                var scheme = entry.Substring(0, separator).Trim();
                var target = entry.Substring(separator + 1).Trim();

                if (scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                    httpTarget = target;
                else if (scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    httpsTarget = target;
                else
                    otherEntries = true;
            }

            var mainTarget = httpTarget ?? httpsTarget;

            if (mainTarget == null || !TryParseHostPort(mainTarget, out host, out port)) {
                host = ProxyConstants.NoProxyWord;
                port = -1;

                return false;
            }

            return !otherEntries && httpTarget != null && httpsTarget != null
                   && string.Equals(httpTarget, httpsTarget, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseHostPort(string value, out string host, out int port)
        {
            host = string.Empty;
            port = -1;

            var separator = value.LastIndexOf(':');

            if (separator <= 0 || separator == value.Length - 1)
                return false;

            if (!int.TryParse(value.Substring(separator + 1), out port))
                return false;

            host = value.Substring(0, separator);

            return true;
        }
    }
}
