// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Apply a throttle policy on the selected exchanges. 
    /// </summary>
    public class ThrottleAction : Action
    {
        public ThrottleChannel ThrottleChannel { get; set; } = ThrottleChannel.All;

        public override FilterScope ActionScope { get; }

        public override string DefaultDescription { get; }

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            return default; 
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
            return default;
        }
    }

    public class ThrottlePolicy
    {
        public ThrottlePolicy(TimeSpan interval, int layer7BandwithBytesPerSeconds)
        {
            Interval = interval;
            Layer7BandwithBytesPerSeconds = layer7BandwithBytesPerSeconds;
        }

        /// <summary>
        /// The window interval where the maximum is evaluated 
        /// </summary>
        public TimeSpan Interval { get; }

        /// <summary>
        /// 
        /// </summary>
        public int Layer7BandwithBytesPerSeconds { get; }
    }


    internal class Throttler
    {
        private readonly IInstantProvider _instantProvider;
        private ReceiveInstant[] _instants = new ReceiveInstant[1024];
        private int _offset = -1;
        private readonly long _checkInterval;
        private readonly long _bandwidthBytesPerMillis;

        public Throttler(ThrottlePolicy proThrottlePolicy, IInstantProvider instantProvider)
        {
            _instantProvider = instantProvider;
            _checkInterval = (long) proThrottlePolicy.Interval.TotalMilliseconds;
            _bandwidthBytesPerMillis = (proThrottlePolicy.Layer7BandwithBytesPerSeconds * 1000L);
        }

        private void GrowBuffer()
        {
            var newBuffer = new ReceiveInstant[(int) (_instants.Length * 1.5)];            
            Array.Copy(_instants, newBuffer, _instants.Length);
        }

        public int ComputeThrottleDelay(int currentReadSize)
        {
            var now = _instantProvider.ElapsedMillis;

            var inInterval = 0L;
            int index; 

            for (index = _offset; index >= 0; index--) {
                var existing = _instants[index];

                if ((now - _checkInterval) < existing.InstantMillis) {
                    inInterval += existing.ReceivedBuffer; 
                }
                else {
                    break;
                }
            }

            // Remove the old instants

            if (index >= 0) {
                var newOffset = index + 1;
                var newLength = _offset - newOffset + 1;
                Array.Copy(_instants, newOffset, _instants, 0, newLength);
                _offset = newOffset;
            }

            var forecast = inInterval + currentReadSize;
            var result = (forecast / (double) _bandwidthBytesPerMillis) - _checkInterval; 

            return result < 0 ? 0 : (int) result;
        }
    }

    internal interface IInstantProvider
    {
        long ElapsedMillis { get;  }
    }


    internal readonly struct ReceiveInstant
    {
        public long InstantMillis { get;  }

        public int ReceivedBuffer { get; }
    }

    
}
