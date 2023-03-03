using System;
using Fluxzy.Clients;
using Fluxzy.Readers;

namespace Fluxzy.Formatters.Metrics
{
    public class ExchangeMetricBuilder
    {
        public ExchangeMetricInfo?  Get(int exchangeId, IArchiveReader reader)
        {
            var exchange = reader.ReadExchange(exchangeId);

            if (exchange == null)
                return null;

            var connectionInfo = reader.ReadConnection(exchange.ConnectionId);

            return new ExchangeMetricInfo(exchangeId, exchange.Metrics, connectionInfo);
        }
    }

    public class ExchangeMetricInfo
    {
        private readonly ConnectionInfo? _connectionInfo;

        public ExchangeMetricInfo(int exchangeId, ExchangeMetrics rawMetrics, ConnectionInfo? connectionInfo)
        {
            _connectionInfo = connectionInfo;
            ExchangeId = exchangeId;
            RawMetrics = rawMetrics;
        }

        public int ExchangeId { get;  }

        public ExchangeMetrics RawMetrics { get;  }

        public bool Available => _connectionInfo != null;

        /// <summary>
        /// Delay until having a connection to the proxy 
        /// </summary>
        public int? Queued => MetricHelper.Diff(RawMetrics.RetrievingPool, RawMetrics.ReceivedFromProxy);
        
        public int? Dns
        {
            get
            {
                if (_connectionInfo == null)
                    return null;

                if (_connectionInfo.DnsSolveStart == default || _connectionInfo.DnsSolveEnd == default)
                    return null;

                return (int)(_connectionInfo.DnsSolveEnd - _connectionInfo.DnsSolveStart).TotalMilliseconds;
            }
        }
        
        public int? TcpHandShake => MetricHelper.Diff(_connectionInfo?.TcpConnectionOpened, _connectionInfo?.TcpConnectionOpening);

        public int ? SslHandShake => MetricHelper.Diff(_connectionInfo?.SslNegotiationEnd, _connectionInfo?.SslNegotiationStart);

        public int? RequestHeader => MetricHelper.Diff(RawMetrics.RequestHeaderSent, RawMetrics.RequestHeaderSending);

        public int? RequestBody => MetricHelper.Diff(RawMetrics.RequestBodySent, RawMetrics.RequestHeaderSent); 

        public int ? Waiting => MetricHelper.Diff(RawMetrics.ResponseHeaderStart, RawMetrics.RequestBodySent); // We need to deduce some kind of ttfb here

        public int? ReceivingHeader => MetricHelper.Diff(RawMetrics.ResponseHeaderEnd, RawMetrics.ResponseHeaderStart);
        
        public int? ReceivingBody => MetricHelper.Diff(RawMetrics.ResponseBodyEnd, RawMetrics.ResponseBodyStart);

        public int? OverAllDuration => MetricHelper.Diff(RawMetrics.ResponseBodyEnd, RawMetrics.ReceivedFromProxy);
    }

    public static class MetricHelper
    {
        public static int ? Diff(DateTime end, DateTime start)
        {
            if (end == default || start == default)
                return null;

            if (start > end)
                return null;

            return (int)(end - start).TotalMilliseconds;
        }
        public static int ? Diff(DateTime? end, DateTime? start)
        {
            return Diff(end ?? default, start ?? default); 
        }
    }
}
