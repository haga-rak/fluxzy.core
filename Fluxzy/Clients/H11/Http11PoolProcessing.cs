// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Clients.H11
{
    internal class Http11PoolProcessing
    {
        private readonly Http11Parser _parser;
        private readonly H1Logger _logger;

        public Http11PoolProcessing(Http11Parser parser, H1Logger logger)
        {
            _parser = parser;
            _logger = logger;
        }
        

        /// <summary>
        /// Process the exchange
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if remote server close connection</returns>
        public async Task<bool> Process(Exchange exchange, byte[] buffer, CancellationToken cancellationToken)
        {
            var bufferRaw = buffer;

            Memory<byte> headerBuffer = bufferRaw;

            exchange.Connection!.AddNewRequestProcessed();
            
            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();
            
            _logger.Trace(exchange.Id, () => $"Begin writing header");
            var headerLength = exchange.Request.Header.WriteHttp11(headerBuffer.Span, true, true);
            
            // Sending request header 

            await exchange.Connection.WriteStream.WriteAsync(headerBuffer.Slice(0, headerLength), cancellationToken);

            _logger.Trace(exchange.Id, () => $"Header sent");

            exchange.Metrics.TotalSent += headerLength;
            exchange.Metrics.RequestHeaderSent = ITimingProvider.Default.Instant();

            // Sending request body 

            if (exchange.Request.Body != null)
            {
                var totalBodySize = await
                    exchange.Request.Body.CopyDetailed(exchange.Connection.WriteStream, 1024 * 16,
                        (_) => { }, cancellationToken).ConfigureAwait(false);
                exchange.Metrics.TotalSent += totalBodySize;
            }

            _logger.Trace(exchange.Id, () => $"Body sent");

            // Waiting for header block 

            var headerBlockDetectResult = await Http11HeaderBlockReader.GetNext(exchange.Connection.ReadStream!, headerBuffer,
                    () => exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant(),
                    () => exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant(),
                    true,
                    cancellationToken);

            Memory<char> headerContent = new char[headerBlockDetectResult.HeaderLength];

            Encoding.ASCII
                .GetChars(headerBuffer.Slice(0, headerBlockDetectResult.HeaderLength).Span, headerContent.Span);
            
            exchange.Response.Header = new ResponseHeader(
                headerContent, exchange.Authority.Secure, _parser);

            _logger.TraceResponse(exchange);

            var shouldCloseConnection =
                exchange.Response.Header.ConnectionCloseRequest
                || exchange.Response.Header.ChunkedBody; // Chunked body response always en with connection close 
            
            if (!exchange.Response.Header.HasResponseBody())
            {
                // We close the connection because
                // many web server still sends a content-body with a 304 response 
                // https://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html 10.3.5

                shouldCloseConnection = true; 

                exchange.Metrics.ResponseBodyStart = 
                    exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();

                exchange.Response.Body = StreamUtils.EmptyStream;

                exchange.ExchangeCompletionSource.TrySetResult(true);

                _logger.Trace(exchange.Id, () => $"No response body");

                return true;
            }

            Stream bodyStream = exchange.Connection.ReadStream;
            
            if (headerBlockDetectResult.HeaderLength < headerBlockDetectResult.TotalReadLength)
            {
                var remainder = new byte[headerBlockDetectResult.TotalReadLength -
                                         headerBlockDetectResult.HeaderLength];
                
                Buffer.BlockCopy(bufferRaw, headerBlockDetectResult.HeaderLength,
                    remainder, 0, remainder.Length);

                // Concat the extra body bytes read while retrieving header
                bodyStream = new CombinedReadonlyStream(
                    shouldCloseConnection,
                    new MemoryStream(remainder),
                    exchange.Connection.ReadStream
                );
            }

            if (exchange.Response.Header.ChunkedBody)
            {
                bodyStream = new ChunkedTransferReadStream(bodyStream, shouldCloseConnection);
            }
       
            if (exchange.Response.Header.ContentLength > 0)
            {
                bodyStream = new ContentBoundStream(bodyStream, exchange.Response.Header.ContentLength);
            }

            exchange.Response.Body =
                new MetricsStream(bodyStream,
                    () =>
                    {
                        exchange.Metrics.ResponseBodyStart = ITimingProvider.Default.Instant();
                        _logger.Trace(exchange.Id, () => $"First body bytes read");
                    },
                    (length) =>
                    {
                        exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                        exchange.Metrics.TotalReceived += length;
                        exchange.ExchangeCompletionSource.SetResult(shouldCloseConnection);
                        _logger.Trace(exchange.Id, () => $"Last body bytes end : {length} total bytes");
                    },
                    (exception) =>
                    {
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