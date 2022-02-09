﻿// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Encoding.Utils;
using Echoes.H2.Helpers;
using Echoes.H2.IO;

namespace Echoes.H2
{
    public class Http11PoolProcessing
    {
        private readonly ITimingProvider _timingProvider;
        private readonly TunnelSetting _tunnelSetting;
        private readonly Http11Parser _parser;
        private readonly Stream _baseConnection;

        private static readonly ReadOnlyMemory<char> Space = " ".AsMemory(); 
        private static readonly ReadOnlyMemory<char> LineFeed = "\r\n".AsMemory(); 
        private static readonly ReadOnlyMemory<char> Protocol = " HTTP/1.1".AsMemory(); 
        private static readonly ReadOnlyMemory<char> HostHeader = "Host: ".AsMemory();


        private static readonly byte[] CrLf = new byte[] { 0x0D, 0x0A, 0x0D, 0x0A };

        public Http11PoolProcessing(
            ITimingProvider timingProvider, 
            TunnelSetting tunnelSetting,
            Http11Parser parser)
        {
            _timingProvider = timingProvider;
            _tunnelSetting = tunnelSetting;
            _parser = parser;
        }

        /// <summary>
        /// Process the exchange
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if remote server close connection</returns>
        public async Task<bool> Process(Exchange exchange, CancellationToken cancellationToken)
        {
            // Here is the opportunity to change header 
            Memory<byte> headerBuffer = new byte[_tunnelSetting.MaxHeaderSize]; 
          
            exchange.Metrics.RequestHeaderSending = _timingProvider.Instant();

            var headerLength = exchange.Request.Header.WriteHttp11(headerBuffer.Span, true);

            await exchange.UpStream.WriteAsync(headerBuffer.Slice(0, headerLength), cancellationToken); 
            
            exchange.Metrics.TotalSent += headerLength;
            exchange.Metrics.RequestHeaderSent = _timingProvider.Instant();

            
            if (exchange.Request.Body  != null)
            {
                var totalBodySize = await
                    exchange.Request.Body.CopyAndReturnCopied(exchange.UpStream, 1024 * 8,
                        (_) => { }, cancellationToken).ConfigureAwait(false);
                exchange.Metrics.TotalSent += totalBodySize;
            }

            var headerBlockDetectResult = await
                DetectHeaderBlock(exchange.UpStream, headerBuffer,
                    () => exchange.Metrics.ResponseHeaderStart = _timingProvider.Instant(),
                    () => exchange.Metrics.ResponseHeaderEnd = _timingProvider.Instant(),
                    cancellationToken);

            Memory<char> headerContent = new char[headerBlockDetectResult.HeaderSize];

            System.Text.Encoding.ASCII
                .GetChars(headerBuffer.Slice(0, headerBlockDetectResult.HeaderSize).Span, headerContent.Span);

            exchange.Response.Header = new ResponseHeader(
                headerContent, exchange.Authority.Secure, _parser);

            var shouldCloseConnection =
                exchange.Response.Header.ConnectionCloseRequest
                || exchange.Response.Header.ChunkedBody; // Chunked body response always en with connection close 

            if (!exchange.Response.Header.HasResponseBody())
                return shouldCloseConnection;


            if (exchange.Response.Header.ChunkedBody)
                exchange.Response.Body = new ChunckedReadStream(exchange.UpStream); 






            return shouldCloseConnection; 


            // Read header from server 

            // Remove non forwardable headers 

            // Stream response 
        }


        /// <summary>
        /// Read header block from input to buffer. Returns the total header length including double CRLF
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="firstByteReceived"></param>
        /// <param name="headerBlockReceived"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async ValueTask<HeaderBlockReadResult>
            DetectHeaderBlock(
            Stream stream, Memory<byte> buffer, Action firstByteReceived, Action headerBlockReceived, CancellationToken token)
        {
            var bufferIndex = buffer;
            var totalRead = 0;
            var indexFound = 0;
            var firstBytes = true;

            while (totalRead < buffer.Length)
            {
                var currentRead = await stream.ReadAsync(bufferIndex, token);

                if (firstBytes)
                {
                    firstByteReceived?.Invoke(); 

                    firstBytes = false;
                }

                var start = totalRead - 4 < 0 ? 0 : (totalRead - 4);

                var searchBuffer = buffer.Slice(start, currentRead + (totalRead - start)); // We should look at that buffer 

                totalRead += currentRead;
                bufferIndex = bufferIndex.Slice(currentRead);

                var detected = searchBuffer.Span.IndexOf(CrLf);

                if (detected >= 0)
                {
                    // FOUND CRLF 

                    indexFound = start + detected + 4;
                    break;
                }
            }

            if (indexFound < 0)
                throw new ExchangeException(
                    $"Double CRLF not detected or header buffer size ({buffer.Length}) is less than actual header size.");

            headerBlockReceived();

            return new HeaderBlockReadResult(indexFound, totalRead);
        }

        
    }
}