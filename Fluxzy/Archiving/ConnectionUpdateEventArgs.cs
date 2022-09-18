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