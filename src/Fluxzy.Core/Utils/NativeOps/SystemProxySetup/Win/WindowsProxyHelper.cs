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
            var proxyServerTab = proxyServer.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            var proxyServerName = proxyServerTab.Length != 2 ? ProxyConstants.NoProxyWord : proxyServerTab[0];
            var proxyPort = proxyServerTab.Length != 2 ? -1 : int.Parse(proxyServerTab[1]);

            var setting = new SystemProxySetting(proxyServerName, proxyPort, proxyOverrideList) {
                Enabled = proxyEnabled
            };

            // Preserve the raw ProxyServer string when it does not match the standard
            // "host:port" format (e.g. legacy per-protocol entries like
            // "http=proxy:80;https=proxy:443"). Without this, the value would be lost
            // on restore because BoundHost collapses to NoProxyWord.
            if (proxyServerTab.Length != 2 && !string.IsNullOrEmpty(proxyServer))
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
                var actualServerLine = $"{systemProxySetting.BoundHost}:{systemProxySetting.ListenPort}";
                registry.SetValue("ProxyServer", actualServerLine);
            }

            var proxyOverrideLine = string.Join(";", systemProxySetting.ByPassHosts);
            registry.SetValue("ProxyOverride", proxyOverrideLine);

            InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
#else
            throw new NotSupportedException("This method is only supported on .NET 6.0 or greater");
#endif
        }
    }
}
