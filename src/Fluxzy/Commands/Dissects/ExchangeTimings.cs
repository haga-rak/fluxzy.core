// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal static class ExchangeTimings
    {
        public static DateTime? Instant(DateTime value)
        {
            return value == default ? null : value;
        }

        /// <summary>Milliseconds from request received to first response header byte.</summary>
        public static double? TtfbMs(ExchangeInfo exchange)
        {
            return ElapsedMs(exchange.Metrics.ReceivedFromProxy, exchange.Metrics.ResponseHeaderStart);
        }

        /// <summary>Milliseconds from request received to end of response body.</summary>
        public static double? DurationMs(ExchangeInfo exchange)
        {
            return ElapsedMs(exchange.Metrics.ReceivedFromProxy, exchange.Metrics.ResponseBodyEnd);
        }

        public static double? ElapsedMs(DateTime start, DateTime end)
        {
            if (start == default || end == default || end < start) {
                return null;
            }

            return Math.Round((end - start).TotalMilliseconds, 2);
        }
    }
}
