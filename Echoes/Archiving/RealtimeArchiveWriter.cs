using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Clients;

namespace Echoes
{
    public abstract class RealtimeArchiveWriter : IArchiveWriter
    {
        public virtual async Task Update(Connection connection, CancellationToken cancellationToken)
        {
            ConnectionInfo connectionInfo = new ConnectionInfo(connection);

            await Update(connectionInfo, cancellationToken); 

        }
        public virtual async Task Update(Exchange exchange, CancellationToken cancellationToken)
        {
            await Update(new ExchangeInfo(exchange), cancellationToken); 
        }

        public abstract Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken);

        public abstract Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken);

        public abstract Stream CreateRequestBodyStream(int exchangeId);

        public abstract Stream CreateResponseBodyStream(int exchangeId);
    }
}