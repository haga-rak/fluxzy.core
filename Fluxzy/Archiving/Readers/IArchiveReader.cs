using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Fluxzy.Readers
{
    public interface IArchiveReader
    {
        IEnumerable<ExchangeInfo> ReadAllExchanges();
        
        ExchangeInfo ReadExchange(int exchangeId);

        IEnumerable<ConnectionInfo> ReadAllConnections();

        ConnectionInfo ReadConnection(int connectionId);

        Stream GetRawCaptureStream(int connectionId);

        Stream GetRequestBody(int exchangeId);

        Stream GetResponseBody(int exchangeId); 
    }
}
