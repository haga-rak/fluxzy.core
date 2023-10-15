// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;

namespace Fluxzy.Core
{
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
