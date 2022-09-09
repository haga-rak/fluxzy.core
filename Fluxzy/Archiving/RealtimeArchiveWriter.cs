using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;

namespace Fluxzy
{
    
    public abstract class RealtimeArchiveWriter : IArchiveWriter
    {
        public event EventHandler<ExchangeUpdateEventArgs> ExchangeUpdated;
        public event EventHandler<ConnectionUpdateEventArgs> ConnectionUpdated;

        public virtual async Task Update(Connection connection, CancellationToken cancellationToken)
        {
            ConnectionInfo connectionInfo = new ConnectionInfo(connection);

            await Update(connectionInfo, cancellationToken); 

            // fire event 
            if (ConnectionUpdated != null)
            {
                ConnectionUpdated(this, new ConnectionUpdateEventArgs(connectionInfo)); 
            }
        }

        public virtual async Task Update(Exchange exchange, UpdateType updateType, CancellationToken cancellationToken)
        {
            var exchangeInfo = new ExchangeInfo(exchange);
            await Update(exchangeInfo, cancellationToken);

            // fire event 
            if (ExchangeUpdated != null)
            {
                ExchangeUpdated(this, new ExchangeUpdateEventArgs(exchangeInfo, exchange, updateType));
            }
        }


        public abstract Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken);

        public abstract Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken);

        public abstract Stream CreateRequestBodyStream(int exchangeId);

        public abstract Stream CreateResponseBodyStream(int exchangeId);

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

            ExchangeUpdated = null;
            ConnectionUpdated = null; 
        }
    }

    public enum UpdateType
    {
        BeforeRequestHeader, 
        AfterResponseHeader, 
        AfterResponse
    }
}