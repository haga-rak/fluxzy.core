// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Fluxzy.Writers
{
    public abstract class RealtimeArchiveWriter
    {
        private int? _maxExchangeCount;
        protected int ErrorCount;
        private Action? _onMaxExchangeCountReached;
        protected long InternalTotalProcessedExchanges;

        public long TotalProcessedExchanges => InternalTotalProcessedExchanges;

        public virtual void Init()
        {
        }

        public virtual void RegisterExchangeLimit(int? maxExchangeCount, Action onMaxExchangeCountReached)
        {
            _maxExchangeCount = maxExchangeCount;
            _onMaxExchangeCountReached = onMaxExchangeCountReached;
        }

        public abstract void UpdateTags(IEnumerable<Tag> tags);

        public abstract bool Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken);

        public abstract void Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken);

        protected abstract void InternalUpdate(DownstreamErrorInfo connectionInfo, CancellationToken cancellationToken);

        public abstract Stream CreateRequestBodyStream(int exchangeId);

        public abstract Stream CreateResponseBodyStream(int exchangeId);

        public abstract Stream CreateWebSocketRequestContent(int exchangeId, int messageId);

        public abstract Stream CreateWebSocketResponseContent(int exchangeId, int messageId);

        public virtual string GetDumpfilePath(int connectionId)
        {
            return string.Empty;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

            ExchangeUpdated = null;
            ConnectionUpdated = null;
            ErrorUpdated = null;
        }

        public event EventHandler<ExchangeUpdateEventArgs>? ExchangeUpdated;

        public event EventHandler<ConnectionUpdateEventArgs>? ConnectionUpdated;

        public event EventHandler<DownstreamErrorEventArgs>? ErrorUpdated;

        public abstract void ClearErrors();

        public virtual void Update(DownstreamErrorInfo errorInfo, CancellationToken cancellationToken)
        {
            InternalUpdate(errorInfo, cancellationToken);

            var currentCount = Interlocked.Increment(ref ErrorCount);

            // fire event 
            if (ErrorUpdated != null)
                ErrorUpdated(this, new DownstreamErrorEventArgs(currentCount));
        }

        public virtual void Update(Connection connection, CancellationToken cancellationToken)
        {
            var connectionInfo = new ConnectionInfo(connection);

            Update(connectionInfo, cancellationToken);

            // fire event 
            if (ConnectionUpdated != null)
                ConnectionUpdated(this, new ConnectionUpdateEventArgs(connectionInfo));
        }

        public virtual void Update(
            Exchange exchange, ArchiveUpdateType updateType,
            CancellationToken cancellationToken)
        {
            var exchangeInfo = new ExchangeInfo(exchange);

            if (updateType == ArchiveUpdateType.AfterResponse) {
                var total = Interlocked.Increment(ref InternalTotalProcessedExchanges);

                if (total == _maxExchangeCount)
                    _onMaxExchangeCountReached?.Invoke();
            }

            if (!Update(exchangeInfo, cancellationToken))
                return; // DO NOT  fire update event when save filter is on

            // fire event 
            if (ExchangeUpdated != null)
                ExchangeUpdated(this, new ExchangeUpdateEventArgs(exchangeInfo, exchange, updateType));
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }

    public enum ArchiveUpdateType
    {
        BeforeRequestHeader,
        AfterResponseHeader,
        AfterResponse,
        WsMessageSent,
        WsMessageReceived,
        Complete
    }
}
