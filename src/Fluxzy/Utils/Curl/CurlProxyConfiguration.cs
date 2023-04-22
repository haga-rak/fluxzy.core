// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Utils.Curl
{
    public class CurlProxyConfiguration : IRunningProxyConfiguration
    {
        public CurlProxyConfiguration(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; }

        public int Port { get; }
    }
}
