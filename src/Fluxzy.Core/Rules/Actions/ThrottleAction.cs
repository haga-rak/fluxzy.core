// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    public class ThrottleAction : Action
    {
        public ThrottleChannel ThrottleChannel { get; set; } = ThrottleChannel.All;

        public override FilterScope ActionScope { get; }

        public override string DefaultDescription { get; }

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.RegisterRequestBodySubstitution();
        }
    }

    [Flags]
    public enum ThrottleChannel
    {
        None,
        Request = 1,
        Response = 2,
        All = Request | Response
    }

    public class ThrottleSubstitution : IStreamSubstitution
    {
        public ValueTask<Stream> Substitute(Stream originalStream)
        {

        }
    }


    public class Throttler
    {
        private readonly int _throttleIntervalMillis;

        private Stopwatch? _watch;
        private long _totalSize; 

        public Throttler(int throttleIntervalMillis)
        {
            _throttleIntervalMillis = throttleIntervalMillis;
        }

        public int GetThrottleDelay(int nextSize)
        {
            _watch ??= Stopwatch.StartNew();

            var elapsed = _watch.ElapsedMilliseconds;

            if (elapsed > 1000) {
                _watch.Restart();
                _totalSize = 0;
            }


        }
    }

    
}
