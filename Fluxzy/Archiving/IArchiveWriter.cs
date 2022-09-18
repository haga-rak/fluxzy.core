using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy
{
    internal interface IArchiveWriter : IDisposable
    {
        Task Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken);

        Task Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken);

        Stream CreateRequestBodyStream(int exchangeId);

        Stream CreateResponseBodyStream(int exchangeId);

        string GetDumpfilePath(int connectionId);
    }
}