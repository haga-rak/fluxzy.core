// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;

namespace Fluxzy.Readers
{
    /// <summary>
    ///  The expected behavior of an archive reader 
    /// </summary>
    public interface IArchiveReader : IDisposable
    {
        /// <summary>
        /// Read meta information from the archive
        /// </summary>
        /// <returns></returns>
        ArchiveMetaInformation ReadMetaInformation();

        /// <summary>
        /// Read all exchanges from the archive
        /// </summary>
        /// <returns></returns>
        IEnumerable<ExchangeInfo> ReadAllExchanges();

        /// <summary>
        ///  Read an exchange from the archive
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        ExchangeInfo? ReadExchange(int exchangeId);

        /// <summary>
        ///  Read all connections from the archive
        /// </summary>
        /// <returns></returns>
        IEnumerable<ConnectionInfo> ReadAllConnections();

        /// <summary>
        /// Read a connection from the archive
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        ConnectionInfo? ReadConnection(int connectionId);

        /// <summary>
        /// Read all downstream errors from the archive
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<DownstreamErrorInfo> ReaderAllDownstreamErrors();

        /// <summary>
        ///  Get a rawcapture stream of a connection
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        Stream? GetRawCaptureStream(int connectionId);

        /// <summary>
        ///  Get a rawcapture key stream of a connection. The content of the stream is an UTF8 string
        ///   in NSS Key log format
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        Stream? GetRawCaptureKeyStream(int connectionId);

        /// <summary>
        ///  Get the request body of an exchange 
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        Stream? GetRequestBody(int exchangeId);

        /// <summary>
        ///  Get the decoded (unchunked and uncompressed) request body of an exchange
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        Stream? GetDecodedRequestBody(int exchangeId);

        /// <summary>
        ///  Get the response body of an exchange
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        long GetRequestBodyLength(int exchangeId);

        /// <summary>
        ///  Get the response body of an exchange
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        long GetResponseBodyLength(int exchangeId);

        /// <summary>
        ///  Get request websocket content 
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        Stream? GetRequestWebsocketContent(int exchangeId, int messageId);

        /// <summary>
        ///  Get response websocket content
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        Stream? GetResponseWebsocketContent(int exchangeId, int messageId);

        /// <summary>
        ///  Get the decoded (unchunked and uncompressed) response body of an exchange
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        Stream? GetDecodedResponseBody(int exchangeId); 

        /// <summary>
        ///  Check if an exchange has a request body
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        bool HasRequestBody(int exchangeId);

        /// <summary>
        ///  Get the response body of an exchange
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        Stream? GetResponseBody(int exchangeId);

        /// <summary>
        ///  Check if an exchange has a response body
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        bool HasResponseBody(int exchangeId);

        /// <summary>
        ///  Check if a connection has a raw capture file
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        bool HasCapture(int connectionId);
    }
}
