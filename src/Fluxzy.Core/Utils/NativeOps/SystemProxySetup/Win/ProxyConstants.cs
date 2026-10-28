// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Utils.NativeOps.SystemProxySetup.Win
{
    internal static class ProxyConstants
    {
        public static readonly string NoProxyWord = "no_proxy_server";

        /// <summary>
        /// Key in <see cref="Fluxzy.Core.Proxy.SystemProxySetting.PrivateValues"/> holding
        /// the raw ProxyServer registry string when it cannot be regenerated from
        /// host/port, so UnRegister restores it verbatim.
        /// </summary>
        public const string WinProxyServerRawKey = "WinProxyServerRaw";
    }
}
