// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
    public interface ITcpConnectionProvider : IAsyncDisposable
    {
        public static ITcpConnectionProvider Default { get; } = new DefaultTcpConnectionProvider();

        ITcpConnection Create(string dumpFileName);
    }
}
