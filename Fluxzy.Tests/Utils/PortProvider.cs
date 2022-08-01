using System;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Fluxzy.Tests.Utils
{
    public static class PortProvider
    {
        private static int _portCounter = Random.Shared.Next(16000, 40000); 

        public static int Next()
        {
            using (new SingleGlobalInstance())
            {
                return NextFreeTcpPort();
            }
        }

        private static int NextFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
           
            return port;
        }
    }

    class SingleGlobalInstance : IDisposable
    {
        private readonly bool _hasHandle;
        private readonly Mutex _mutex;

        public SingleGlobalInstance()
        {
            string appGuid = "echoes-unit-tests";

            string mutexId = $"Global\\{{{appGuid}}}";

            _mutex = new Mutex(false, mutexId);

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            _mutex.SetAccessControl(securitySettings);

            try
            {
                _hasHandle = _mutex.WaitOne(Timeout.Infinite, false);

                if (_hasHandle == false)
                    throw new TimeoutException("Timeout waiting for exclusive access on SingleInstance");
            }
            catch (AbandonedMutexException)
            {
                _hasHandle = true;
            }
        }


        public void Dispose()
        {
            if (_hasHandle)
                _mutex.ReleaseMutex();

            _mutex.Close();
        }
    }
}