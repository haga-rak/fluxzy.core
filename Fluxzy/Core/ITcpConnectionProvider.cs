// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Core
{
    public interface ITcpConnectionProvider
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
    }
}