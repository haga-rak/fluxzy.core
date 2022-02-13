using System.Collections.Generic;
using System.Threading;

namespace Echoes.Core
{
    internal class ConnectionStateKey
    {
        public ConnectionStateKey(string hostname, int port, bool secure)
        {
            Hostname = hostname;
            Port = port;
            Secure = secure;
        }

        public string Hostname { get;  }

        public int Port { get;  }

        public bool Secure { get;  }

        public override bool Equals(object obj)
        {
            return obj is ConnectionStateKey key &&
                   Hostname == key.Hostname &&
                   Port == key.Port &&
                   Secure == key.Secure;
        }

        public override int GetHashCode()
        {
            var hashCode = -876487182;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Hostname);
            hashCode = hashCode * -1521134295 + Port.GetHashCode();
            hashCode = hashCode * -1521134295 + Secure.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"CSK : {Hostname}:{Port}";
        }
    }

    internal class ConnectionState
    {
        public ConnectionState()
        {

        }


        private int _currentRunning = 0;

        public int CurrentRunning
        {
            get => _currentRunning;
            set => _currentRunning = value;
        }

        public bool AnticipationEngaged { get; set; } = false;

        public void SafeIncrement()
        {
            Interlocked.Increment(ref _currentRunning);
        }

        public void SafeDecrement()
        {
            Interlocked.Decrement(ref _currentRunning);
        }

        public SemaphoreSlim AccessSemaphore { get; } = new SemaphoreSlim(1, 1);


        public SemaphoreSlim WaitNewConnectionSemaphore { get; } = new SemaphoreSlim(0);


        public HashSet<IUpstreamConnection> AvailableConnections { get; set; } = new HashSet<IUpstreamConnection>();

        public override string ToString()
        {
            return $"Cx : {_currentRunning}";
        }
    }
}