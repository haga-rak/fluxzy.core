// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    /// <summary>
    /// Connection update event arguments
    /// </summary>
    public class ConnectionUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// Create a new instance from a ConnectionInfo
        /// </summary>
        /// <param name="connection"></param>
        public ConnectionUpdateEventArgs(ConnectionInfo connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// The updated connection
        /// </summary>
        public ConnectionInfo Connection { get; }
    }
}
