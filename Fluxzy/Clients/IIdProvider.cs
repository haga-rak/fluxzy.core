// Copyright © 2022 Haga Rakotoharivelo

using System.Threading;

namespace Fluxzy.Clients
{
    public interface IIdProvider
    {
        int NextExchangeId();

        public static IIdProvider FromZero => new FromIndexIdProvider(0,0); 
    }

    public class FromIndexIdProvider : IIdProvider
    {
        private volatile int _exchangeIdStart;
        private volatile int _connectionIdStart;

        public FromIndexIdProvider(int exchangeIdStart, int connectionIdStart)
        {
            _exchangeIdStart = exchangeIdStart;
            _connectionIdStart = connectionIdStart;
        }

        public int NextExchangeId()
        {
            return Interlocked.Increment(ref _exchangeIdStart); 
        }

        public int NextConnectionId()
        {
            return Interlocked.Increment(ref _connectionIdStart); 
        }

        /// <summary>
        /// Set next exchange id to this value
        /// </summary>
        /// <param name="value"></param>
        public void SetNextExchangeId(int value)
        {
            _exchangeIdStart = value; 
        }

        /// <summary>
        /// Set next exchange id to this value
        /// </summary>
        /// <param name="value"></param>
        public void SetNextConnectionId(int value)
        {
            _connectionIdStart = value; 
        }


    }
}