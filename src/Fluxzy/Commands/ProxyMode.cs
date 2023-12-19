// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Cli.Commands
{
    public enum ProxyMode
    {
        /// <summary>
        ///  Act as normal proxy 
        /// </summary>
        Regular = 1,

        /// <summary>
        /// Act as reverse proxy and expect plain HTTP/1.1 from the client
        /// </summary>
        ReversePlain,

        /// <summary>
        /// Act as reverse proxy and expect TLS from the client
        /// </summary>
        ReverseSecure
    }
}
