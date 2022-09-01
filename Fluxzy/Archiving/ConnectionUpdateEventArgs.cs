using System;
using Fluxzy.Clients;

namespace Fluxzy
{
    public class ConnectionUpdateEventArgs : EventArgs
    {
        public ConnectionInfo Connection { get; }

        public ConnectionUpdateEventArgs(ConnectionInfo connection)
        {
            Connection = connection;
        }
    }
}