using System;
using System.Collections.Generic;
using System.IO;

namespace Fluxzy.Readers
{
    public interface IArchiveReader : IDisposable
    {
        ArchiveMetaInformation ReadMetaInformation();

        IEnumerable<ExchangeInfo> ReadAllExchanges();

        ExchangeInfo? ReadExchange(int exchangeId);

        IEnumerable<ConnectionInfo> ReadAllConnections();

        ConnectionInfo? ReadConnection(int connectionId);

        Stream? GetRawCaptureStream(int connectionId);

        Stream? GetRequestBody(int exchangeId);

        Stream? GetRequestWebsocketContent(int exchangeId, int messageId);

        Stream? GetResponseWebsocketContent(int exchangeId, int messageId);

        bool HasRequestBody(int exchangeId);

        Stream? GetResponseBody(int exchangeId);

        bool HasResponseBody(int exchangeId);
    }
}
