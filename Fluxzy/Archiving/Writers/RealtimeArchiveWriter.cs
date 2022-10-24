using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;

namespace Fluxzy.Writers
{
    public abstract class RealtimeArchiveWriter
    {
        public virtual void Init()
        {

        }

        public abstract void UpdateTags(IEnumerable<Tag> tags);

        public abstract void Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken);

        public abstract void Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken);

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
        }

        public event EventHandler<ExchangeUpdateEventArgs>? ExchangeUpdated;
        public event EventHandler<ConnectionUpdateEventArgs>? ConnectionUpdated;

        public virtual void Update(Connection connection, CancellationToken cancellationToken)
        {
            var connectionInfo = new ConnectionInfo(connection);

            Update(connectionInfo, cancellationToken);

            // fire event 
            if (ConnectionUpdated != null)
                ConnectionUpdated(this, new ConnectionUpdateEventArgs(connectionInfo));
        }

        public virtual void Update(Exchange exchange, UpdateType updateType,
            CancellationToken cancellationToken)
        {
            var exchangeInfo = new ExchangeInfo(exchange);

            Update(exchangeInfo, cancellationToken);

            // fire event 
            if (ExchangeUpdated != null)
                ExchangeUpdated(this, new ExchangeUpdateEventArgs(exchangeInfo, exchange, updateType));
        }

        protected virtual void Dispose(bool disposing)
        {

        }
    }

    public enum UpdateType
    {
        BeforeRequestHeader,
        AfterResponseHeader,
        AfterResponse,
        WsMessageSent,
        WsMessageReceived,
        Complete,
    }
}