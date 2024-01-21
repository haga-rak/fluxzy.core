// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Fluxzy.Writers
{
    /// <summary>
    /// Represents a base class for writing real-time archive data.
    /// </summary>
    public abstract class RealtimeArchiveWriter
    {
        private int? _maxExchangeCount;
        protected int ErrorCount;
        private Action? _onMaxExchangeCountReached;
        protected long InternalTotalProcessedExchanges;

        /// <summary>
        /// Gets the total number of exchanges that have been processed.
        /// </summary>
        public long TotalProcessedExchanges => InternalTotalProcessedExchanges;

        public virtual void Init()
        {
        }

        /// <summary>
        /// Registers the exchange limit for the system.
        /// </summary>
        /// <param name="maxExchangeCount">The maximum number of exchanges allowed. Pass null for unlimited exchanges.</param>
        /// <param name="onMaxExchangeCountReached">The action to be invoked when the maximum exchange count is reached.</param>
        public virtual void RegisterExchangeLimit(int? maxExchangeCount, Action onMaxExchangeCountReached)
        {
            _maxExchangeCount = maxExchangeCount;
            _onMaxExchangeCountReached = onMaxExchangeCountReached;
        }

        /// <summary>
        /// Updates the tags associated with the item.
        /// </summary>
        /// <param name="tags">The tags to be updated.</param>
        public abstract void UpdateTags(IEnumerable<Tag> tags);

        /// <summary>
        /// Updates the exchange information.
        /// </summary>
        /// <param name="exchangeInfo">The exchange information to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns a value indicating whether the update was successful.</returns>
        public abstract bool Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the connection information for the given connection.
        /// </summary>
        /// <param name="connectionInfo">The new connection information to be updated.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the update operation.</param>
        public abstract void Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken);

        /// <summary>
        /// Performs the internal update logic for the specified connection.
        /// </summary>
        /// <param name="connectionInfo">The information about the connection.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        protected abstract void InternalUpdate(DownstreamErrorInfo connectionInfo, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a request body stream for the specified exchange ID.
        /// </summary>
        /// <param name="exchangeId">The ID of the exchange.</param>
        /// <returns>A stream representing the request body.</returns>
        public abstract Stream CreateRequestBodyStream(int exchangeId);

        /// <summary>
        /// Creates the response body stream for the given exchange ID.
        /// </summary>
        /// <param name="exchangeId">The identifier of the exchange.</param>
        /// <returns>The response body stream.</returns>
        public abstract Stream CreateResponseBodyStream(int exchangeId);

        /// <summary>
        /// Creates the content of a WebSocket request.
        /// </summary>
        /// <param name="exchangeId">The ID of the exchange.</param>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>A <see cref="Stream"/> representing the content of the WebSocket request.</returns>
        public abstract Stream CreateWebSocketRequestContent(int exchangeId, int messageId);

        /// <summary>
        /// Creates the content for the WebSocket response.
        /// </summary>
        /// <param name="exchangeId">The ID of the exchange.</param>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>A Stream containing the response content.</returns>
        public abstract Stream CreateWebSocketResponseContent(int exchangeId, int messageId);

        /// <summary>
        /// Gets the dump file path for the specified connection ID.
        /// </summary>
        /// <param name="connectionId">The ID of the connection.</param>
        /// <returns>The dump file path as a string.</returns>
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

        /// <summary>
        /// Represents an event that is raised when an exchange is updated.
        /// </summary>
        /// <remarks>
        /// This event provides a notification whenever an exchange is updated.
        /// </remarks>
        public event EventHandler<ExchangeUpdateEventArgs>? ExchangeUpdated;

        /// <summary>
        /// Represents an event that is raised when a connection is updated.
        /// </summary>
        public event EventHandler<ConnectionUpdateEventArgs>? ConnectionUpdated;

        /// <summary>
        /// Represents an event that is raised when the error is updated downstream.
        /// </summary>
        public event EventHandler<DownstreamErrorEventArgs>? ErrorUpdated;

        public abstract void ClearErrors();

        /// <summary>
        /// Updates the downstream error information and triggers an event when the error count is increased.
        /// </summary>
        /// <param name="errorInfo">The new downstream error information to update.</param>
        /// <param name="cancellationToken">A token to cancel the update operation.</param>
        public virtual void Update(DownstreamErrorInfo errorInfo, CancellationToken cancellationToken)
        {
            InternalUpdate(errorInfo, cancellationToken);

            var currentCount = Interlocked.Increment(ref ErrorCount);

            // fire event 
            if (ErrorUpdated != null)
                ErrorUpdated(this, new DownstreamErrorEventArgs(currentCount));
        }

        /// <summary>
        /// Updates the specified connection.
        /// </summary>
        /// <param name="connection">The connection to be updated.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        public virtual void Update(Connection connection, CancellationToken cancellationToken)
        {
            var connectionInfo = new ConnectionInfo(connection);

            Update(connectionInfo, cancellationToken);

            // fire event 
            if (ConnectionUpdated != null)
                ConnectionUpdated(this, new ConnectionUpdateEventArgs(connectionInfo));
        }

        /// <summary>
        /// Updates an exchange with the specified update type.
        /// </summary>
        /// <param name="exchange">The exchange to update.</param>
        /// <param name="updateType">The type of update to perform.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the update operation.</param>
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

    /// <summary>
    /// Represents the type of archive update.
    /// </summary>
    public enum ArchiveUpdateType
    {
        BeforeRequestHeader,
        AfterResponseHeader,
        AfterResponse,
        WsMessageSent,
        WsMessageReceived
    }
}
