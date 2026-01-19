// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading;

namespace Fluxzy.Cli.Commands.PrettyOutput
{
    /// <summary>
    /// Thread-safe statistics aggregator for proxy traffic.
    /// Uses Interlocked operations for lock-free thread safety.
    /// </summary>
    public class ProxyStatistics
    {
        private long _totalExchanges;
        private long _successCount;      // 2xx responses
        private long _errorCount;        // Non-2xx responses
        private long _totalDownloaded;   // bytes received
        private long _totalUploaded;     // bytes sent
        private int _activeConnections;

        /// <summary>
        /// Total number of completed exchanges.
        /// </summary>
        public long TotalExchanges => Interlocked.Read(ref _totalExchanges);

        /// <summary>
        /// Number of successful exchanges (2xx status codes).
        /// </summary>
        public long SuccessCount => Interlocked.Read(ref _successCount);

        /// <summary>
        /// Number of error exchanges (non-2xx status codes or transport errors).
        /// </summary>
        public long ErrorCount => Interlocked.Read(ref _errorCount);

        /// <summary>
        /// Total bytes downloaded from upstream servers.
        /// </summary>
        public long TotalDownloaded => Interlocked.Read(ref _totalDownloaded);

        /// <summary>
        /// Total bytes uploaded to upstream servers.
        /// </summary>
        public long TotalUploaded => Interlocked.Read(ref _totalUploaded);

        /// <summary>
        /// Current number of active connections.
        /// </summary>
        public int ActiveConnections => Volatile.Read(ref _activeConnections);

        /// <summary>
        /// Records a completed exchange.
        /// </summary>
        /// <param name="statusCode">HTTP status code (0 if no response)</param>
        /// <param name="downloaded">Bytes received</param>
        /// <param name="uploaded">Bytes sent</param>
        /// <param name="hasTransportError">True if the exchange had a transport-level error</param>
        public void RecordExchange(int statusCode, long downloaded, long uploaded, bool hasTransportError)
        {
            Interlocked.Increment(ref _totalExchanges);
            Interlocked.Add(ref _totalDownloaded, downloaded);
            Interlocked.Add(ref _totalUploaded, uploaded);

            var isSuccess = statusCode >= 200 && statusCode < 300 && !hasTransportError;

            if (isSuccess)
            {
                Interlocked.Increment(ref _successCount);
            }
            else
            {
                Interlocked.Increment(ref _errorCount);
            }
        }

        /// <summary>
        /// Records a completed exchange from an ExchangeDisplayEntry.
        /// </summary>
        public void RecordExchange(ExchangeDisplayEntry entry)
        {
            RecordExchange(entry.StatusCode, entry.Size, 0, entry.HasError);
        }

        /// <summary>
        /// Increments the active connection count.
        /// </summary>
        public void IncrementConnections()
        {
            Interlocked.Increment(ref _activeConnections);
        }

        /// <summary>
        /// Decrements the active connection count.
        /// </summary>
        public void DecrementConnections()
        {
            Interlocked.Decrement(ref _activeConnections);
        }

        /// <summary>
        /// Resets all statistics to zero.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _totalExchanges, 0);
            Interlocked.Exchange(ref _successCount, 0);
            Interlocked.Exchange(ref _errorCount, 0);
            Interlocked.Exchange(ref _totalDownloaded, 0);
            Interlocked.Exchange(ref _totalUploaded, 0);
            // Note: We don't reset active connections as they represent live state
        }
    }
}
