// Copyright © 2022 Haga Rakotoharivelo

using System.Threading;

namespace Fluxzy.Clients
{
    public interface IIdProvider
    {
        int NextId();

        public static IIdProvider FromZero => new FromIndexIdProvider(0); 
    }

    public class FromIndexIdProvider : IIdProvider
    {
        private int _start;

        public FromIndexIdProvider(int start)
        {
            _start = start;
        }

        public int NextId()
        {
            return Interlocked.Increment(ref _start); 
        }
    }
}