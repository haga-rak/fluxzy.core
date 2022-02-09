// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Diagnostics;

namespace Echoes.H2
{
    public interface ITimingProvider
    {
        class DefaultTimingProvider : ITimingProvider
        {
            private readonly DateTime _start;
            private readonly Stopwatch _watch = new Stopwatch();

            public DefaultTimingProvider()
            {
                _start = DateTime.UtcNow;
                _watch.Start();
            }

            public DateTime Instant()
            {
                return _start.Add(_watch.Elapsed);
            }
        }

        public static ITimingProvider Default { get; } = new DefaultTimingProvider(); 

        DateTime Instant(); 
    }
}