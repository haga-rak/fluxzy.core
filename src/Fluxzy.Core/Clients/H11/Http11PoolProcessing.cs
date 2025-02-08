// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.H11
{
    internal class Http11PoolProcessing
    {
        private readonly H1Logger _logger;

        public Http11PoolProcessing(H1Logger logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///     Process the exchange
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if remote server close connection</returns>
        public async ValueTask<bool> Process(Exchange exchange, RsBuffer buffer, CancellationToken cancellationToken)
        {
            if (exchange.Context.EventNotifierStream?.Faulted == true) {
                throw new ConnectionCloseException("Abandoned stream");
            }

            exchange.Connection!.AddNewRequestProcessed();

            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();

            _logger.Trace(exchange.Id, () => "Begin writing header");

            var headerLength = exchange.Request.Header.WriteHttp11(
                !exchange.Authority.Secure,
                buffer.Buffer, true, true, true);

            // Sending request header 

            await exchange.Connection.WriteStream!
                          .WriteAsync(buffer.Memory.Slice(0, headerLength), cancellationToken)
                          .ConfigureAwait(false);

            _logger.Trace(exchange.Id, () => "Header sent");

            exchange.Metrics.TotalSent += headerLength;
            exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();
            exchange.Metrics.RequestHeaderLength = headerLength;

            // Sending request body 

            if (exchange.Request.Body != null) {
                var writeStream = exchange.Connection.WriteStream;
                ChunkedTransferWriteStream? chunkedStream = null;

                if (exchange.Request.Header.ChunkedBody) {
                    writeStream = chunkedStream = new ChunkedTransferWriteStream(writeStream);
                }

                var totalBodySize = await
                    exchange.Request.Body.CopyDetailed(writeStream, 1024 * 16,
                        _ => { }, cancellationToken).ConfigureAwait(false);

                exchange.Metrics.TotalSent += totalBodySize;

                if (chunkedStream != null) {
                    await chunkedStream.WriteEof().ConfigureAwait(false);
                }
            }

            exchange.Metrics.RequestBodySent = ITimingProvider.Default.Instant();

            _logger.Trace(exchange.Id, () => "Body sent");

            // Waiting for header block 

            HeaderBlockReadResult headerBlockDetectResult = default;

            try {
                while (true) {
                    headerBlockDetectResult = await Http11HeaderBlockReader.GetNext(exchange.Connection.ReadStream!,
                        buffer,
                        () => exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant(),
                        () => exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant(),
                        true,
                        cancellationToken,
                        true).ConfigureAwait(false);

                    var is100Continue = HttpHelper.Is100Continue(
                        buffer.Buffer);

                    if (is100Continue) {
                        // Even if fluxzy is not sending expect 100-continue,
                        // we still need to handle it as many apache based server
                        // will send it anyway

                        continue;
                    }

                    break;
                }
            }
            catch (Exception ex) {
                if (ex is TlsFatalAlert || (exchange.Context.EventNotifierStream?.Faulted ?? false)) {
                    throw new ConnectionCloseException("Relaunch");
                }

                throw new ClientErrorException(0,
                    "The connection was closed while trying to read the response header",
                    ex.Message, ex);
            }

            if (headerBlockDetectResult.CloseNotify) {
                throw new ConnectionCloseException("Relaunch");
            }

            Memory<char> headerContent = new char[headerBlockDetectResult.HeaderLength];

            Encoding.ASCII
                    .GetChars(buffer.Memory.Slice(0, headerBlockDetectResult.HeaderLength).Span, headerContent.Span);

            exchange.Response.Header = new ResponseHeader(
                headerContent, exchange.Authority.Secure, true);

            _logger.TraceResponse(exchange);

            var shouldCloseConnection =
                    exchange.Response.Header.ConnectionCloseRequest
                ; //|| exchange.Response.Header.ChunkedBody; 

            if (!exchange.Response.Header.HasResponseBody(exchange.Request.Header.Method.Span, out var shouldClose)) {
                // We close the connection because
                // many web server still sends a content-body with a 304 response 
                // https://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html 10.3.5

                exchange.Metrics.ResponseBodyStart =
                    exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();

                exchange.Response.Body = Stream.Null;

                exchange.ExchangeCompletionSource.TrySetResult(shouldClose);

                _logger.Trace(exchange.Id, () => "No response body");

                return true;
            }

            var bodyStream = exchange.Connection.ReadStream!;

            if (headerBlockDetectResult.HeaderLength < headerBlockDetectResult.TotalReadLength) {
                var length = headerBlockDetectResult.TotalReadLength -
                             headerBlockDetectResult.HeaderLength;

                // Concat the extra body bytes read while retrieving header
                bodyStream = new CombinedReadonlyStream(
                    shouldCloseConnection,
                    buffer.Buffer.AsSpan(headerBlockDetectResult.HeaderLength, length),
                    exchange.Connection.ReadStream!
                );
            }

            exchange.Metrics.TotalReceived += headerBlockDetectResult.HeaderLength;
            exchange.Metrics.ResponseHeaderLength = headerBlockDetectResult.HeaderLength;

            if (exchange.Response.Header.ChunkedBody) {
                bodyStream = new ChunkedTransferReadStream(bodyStream, shouldCloseConnection);
            }

            if (exchange.Response.Header.ContentLength > 0) {
                bodyStream = new ContentBoundStream(bodyStream, exchange.Response.Header.ContentLength);
            }

            exchange.Response.Body =
                new MetricsStream(bodyStream,
                    () => {
                        exchange.Metrics.ResponseBodyStart = ITimingProvider.Default.Instant();
                        _logger.Trace(exchange.Id, () => "First body bytes read");
                    },
                    length => {
                        exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                        exchange.Metrics.TotalReceived += length;
                        exchange.ExchangeCompletionSource.SetResult(shouldCloseConnection);

                        _logger.Trace(exchange.Id, () => $"Last body bytes end : {length} total bytes");
                    },
                    exception => {
                        exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                        exchange.ExchangeCompletionSource.SetException(exception);

                        _logger.Trace(exchange.Id, () => $"Read error : {exception}");
                    },
                    cancellationToken
                )
                ;

            return shouldCloseConnection;
        }
    }
}
