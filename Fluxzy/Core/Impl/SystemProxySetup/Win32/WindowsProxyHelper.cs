using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Fluxzy.Core.SystemProxySetup.Win32
{
    internal static class WindowsProxyHelper
    {
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int InternetOptionSettingsChanged = 39;
        private const int InternetOptionRefresh = 37;

        internal static ProxySetting GetSetting()
        {
            using (var registry =
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
                    true))
            {
                if (registry == null)
                    throw new InvalidOperationException("Unable to access system registry"); ;

                var proxyEnabled = (int)registry.GetValue("ProxyEnable", 0) == 1;
                var proxyOverride = (string)registry.GetValue("ProxyOverride", string.Empty);
                var proxyServer = (string)registry.GetValue("ProxyServer", string.Empty);

                var proxyOverrideList = proxyOverride.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                var proxyServerTab = proxyServer.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

                var proxyServerName = proxyServerTab.Length != 2 ? null : proxyServerTab[0];
                var proxyPort = proxyServerTab.Length != 2 ? -1 : int.Parse(proxyServerTab[1]);
                
                return new ProxySetting(proxyServerName, proxyPort, proxyEnabled, proxyOverrideList);
            }
        }

        internal static void SetProxySetting(ProxySetting proxySetting)
        {
            using var registry =
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings",
                    true);

            if (registry == null)
                throw new InvalidOperationException("Unable to access system registry"); ;

            registry.SetValue("ProxyEnable", proxySetting.Enabled ? 1 : 0);

            if (proxySetting.BoundHost == null)
            {
                // Remove proxy setting 
                registry.DeleteValue("ProxyServer");
            }
            else
            {
                var actualServerLine = $"{proxySetting.BoundHost}:{proxySetting.ListenPort}";
                registry.SetValue("ProxyServer", actualServerLine);
            }

            var proxyOverrideLine = string.Join(";", proxySetting.ByPassHosts);
            registry.SetValue("ProxyOverride", proxyOverrideLine);

            InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
        }
    }
}