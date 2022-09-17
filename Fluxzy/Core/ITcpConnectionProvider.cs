// Copyright © 2022 Haga Rakotoharivelo

using System;

namespace Fluxzy.Core
{
    public interface ITcpConnectionProvider : IDisposable
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

        public void Dispose()
        {

        }
    }
}