// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;

namespace Fluxzy.Core
{
    public class DefaultTcpConnectionProvider : ITcpConnectionProvider
    {
        public ITcpConnection Create(string _)
        {
            return new DefaultTcpConnection();
        }

        public void TryFlush()
        {
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
