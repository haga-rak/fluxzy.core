// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
    public interface ITcpConnectionProvider : IAsyncDisposable
    {
        public static ITcpConnectionProvider Default { get; } = new DefaultTcpConnectionProvider();

        ITcpConnection Create(string dumpFileName);

        void TryFlush();

        /// <summary>
        ///     When true, this provider cannot function without a writable archive and may be ignored
        ///     when the archiving policy is None. Custom providers return false and are always honored.
        /// </summary>
        bool RequiresArchiveWriter => false;
    }
}
