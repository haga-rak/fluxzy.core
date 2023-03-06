// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

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

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
