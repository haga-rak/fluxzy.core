// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy
{
    /// <summary>
    ///    Represents an authority (host, port, secure) for a request
    /// </summary>
    public interface IAuthority
    {
        /// <summary>
        ///   The host name
        /// </summary>
        string HostName { get; }

        /// <summary>
        ///  The port number
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Whether the connection is secure or not
        /// </summary>
        bool Secure { get; }
    }
}
