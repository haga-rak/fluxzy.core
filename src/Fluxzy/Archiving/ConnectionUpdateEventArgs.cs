// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    public class ConnectionUpdateEventArgs : EventArgs
    {
        public ConnectionUpdateEventArgs(ConnectionInfo connection)
        {
            Connection = connection;
        }

        public ConnectionInfo Connection { get; }
    }
}
