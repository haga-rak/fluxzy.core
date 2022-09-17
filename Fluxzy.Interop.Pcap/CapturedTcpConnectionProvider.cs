// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Core;

namespace Fluxzy.Interop.Pcap
{
    public class CapturedTcpConnectionProvider : ITcpConnectionProvider
    {
        public ITcpConnection Create(string dumpFileName)
        {
            return new CapturedTcpConnection(dumpFileName); 
        }
    }
}