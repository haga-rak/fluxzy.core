// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Utils.NativeOps.SystemProxySetup.Win
{
    internal static class ProxyConstants
    {
        public static readonly string NoProxyWord = "no_proxy_server";

        /// <summary>
        /// Key used in <see cref="Fluxzy.Core.Proxy.SystemProxySetting.PrivateValues"/>
        /// to preserve the raw ProxyServer registry string when it cannot be parsed
        /// as the standard "host:port" format (e.g. legacy per-protocol entries
        /// like "http=proxy:80;https=proxy:443"). This allows UnRegister to
        /// restore the original string verbatim instead of corrupting it.
        /// </summary>
        public const string WinProxyServerRawKey = "WinProxyServerRaw";
    }
}
