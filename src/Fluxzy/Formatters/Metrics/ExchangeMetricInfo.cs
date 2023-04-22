// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients;

namespace Fluxzy.Formatters.Metrics
{
    public class ExchangeMetricInfo
    {
        private readonly ConnectionInfo? _connectionInfo;

        public ExchangeMetricInfo(int exchangeId, ExchangeMetrics rawMetrics, ConnectionInfo? connectionInfo)
        {
            _connectionInfo = connectionInfo;
            ExchangeId = exchangeId;
            RawMetrics = rawMetrics;
        }

        public int ExchangeId { get; }

        public ExchangeMetrics RawMetrics { get; }

        public bool Available => _connectionInfo != null;

        /// <summary>
        ///     Delay until having a connection to the proxy
        /// </summary>
        public int? Queued => MetricHelper.Diff(RawMetrics.RetrievingPool, RawMetrics.ReceivedFromProxy);

        public int? Dns => MetricHelper.Diff(_connectionInfo?.DnsSolveEnd, _connectionInfo?.DnsSolveStart);

        public int? TcpHandShake =>
            MetricHelper.Diff(_connectionInfo?.TcpConnectionOpened, _connectionInfo?.TcpConnectionOpening);

        public int? SslHandShake =>
            MetricHelper.Diff(_connectionInfo?.SslNegotiationEnd, _connectionInfo?.SslNegotiationStart);

        public int? RequestHeader => MetricHelper.Diff(RawMetrics.RequestHeaderSent, RawMetrics.RequestHeaderSending);

        public int? RequestBody => MetricHelper.Diff(RawMetrics.RequestBodySent, RawMetrics.RequestHeaderSent);

        public int? Waiting =>
            MetricHelper.Diff(RawMetrics.ResponseHeaderStart,
                RawMetrics.RequestBodySent); // We need to deduce some kind of ttfb here

        public int? ReceivingHeader => MetricHelper.Diff(RawMetrics.ResponseHeaderEnd, RawMetrics.ResponseHeaderStart);

        public int? ReceivingBody => MetricHelper.Diff(RawMetrics.ResponseBodyEnd, RawMetrics.ResponseHeaderEnd);

        public int? OverAllDuration => MetricHelper.Diff(RawMetrics.ResponseBodyEnd, RawMetrics.ReceivedFromProxy);
    }

    internal static class MetricHelper
    {
        public static int? Diff(DateTime end, DateTime start)
        {
            if (end == default || start == default)
                return null;

            if (start > end)
                return null;

            return (int) ((end - start).TotalMilliseconds * 1000);
        }

        public static int? Diff(DateTime? end, DateTime? start)
        {
            return Diff(end ?? default, start ?? default);
        }
    }
}
