// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    public interface ITcpConnectionProvider : IAsyncDisposable
    {
        public static ITcpConnectionProvider Default { get; } = new DefaultTcpConnectionProvider();

        ITcpConnection Create(string dumpFileName); 
    }

    public class DefaultTcpConnectionProvider : ITcpConnectionProvider
    {
        public ITcpConnection Create(string dumpFileName)
        {
            return new DefaultTcpConnection();
        }
        
        public async ValueTask DisposeAsync()
        {
        }
    }
}