// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Encoding.Utils;
using Echoes.H2.IO;

namespace Echoes.H2
{
    public class Http11PoolProcessing
    {
        private readonly ITimingProvider _timingProvider;
        private readonly Http11Parser _parser;
        private readonly Stream _baseConnection;

        private static readonly ReadOnlyMemory<char> Space = " ".AsMemory(); 
        private static readonly ReadOnlyMemory<char> LineFeed = "\r\n".AsMemory(); 
        private static readonly ReadOnlyMemory<char> Protocol = " HTTP/1.1".AsMemory(); 
        private static readonly ReadOnlyMemory<char> HostHeader = "Host: ".AsMemory(); 

        public Http11PoolProcessing(ITimingProvider timingProvider, Http11Parser parser)
        {
            _timingProvider = timingProvider;
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
            // Read headers from base stream or from provisional data 
            var authority =
                exchange.Request.Headers
                    .First(t => t.Name.Span.Equals(":authority".AsSpan(), StringComparison.Ordinal));

            var method =
                exchange.Request.Headers
                    .First(t => t.Name.Span.Equals(":method".AsSpan(), StringComparison.Ordinal));

            var path =
                exchange.Request.Headers
                    .First(t => t.Name.Span.Equals(":path".AsSpan(), StringComparison.Ordinal));

            // Here is the opportunity to change header 

            await using var counterStream = new CounterStream(exchange.UpStream);
            await using (var streamWriter = new StreamWriter(counterStream, System.Text.Encoding.ASCII, 1024 * 4, true))
            {
                exchange.Metrics.RequestHeaderSending = _timingProvider.Instant();

                await streamWriter.WriteAsync(method.Value, cancellationToken).ConfigureAwait(false);
                await streamWriter.WriteAsync(Space, cancellationToken).ConfigureAwait(false);
                await streamWriter.WriteAsync(path.Value, cancellationToken).ConfigureAwait(false);
                await streamWriter.WriteAsync(Protocol, cancellationToken).ConfigureAwait(false);
                await streamWriter.WriteAsync(LineFeed, cancellationToken).ConfigureAwait(false);

                await streamWriter.WriteAsync(HostHeader, cancellationToken).ConfigureAwait(false);
                await streamWriter.WriteAsync(authority.Value, cancellationToken).ConfigureAwait(false);
                await streamWriter.WriteAsync(LineFeed, cancellationToken).ConfigureAwait(false);

                foreach (var header in exchange.Request.Headers)
                {
                    if (header.Name.Span[0] == ':')
                        continue; // H2 control headers  

                    await streamWriter.WriteAsync(header.Name, cancellationToken).ConfigureAwait(false);
                    await streamWriter.WriteAsync(Space, cancellationToken).ConfigureAwait(false);
                    await streamWriter.WriteAsync(header.Value, cancellationToken).ConfigureAwait(false);
                    await streamWriter.WriteAsync(LineFeed, cancellationToken).ConfigureAwait(false);
                }

                await streamWriter.WriteAsync(LineFeed, cancellationToken).ConfigureAwait(false);

                exchange.Metrics.RequestHeaderSent = _timingProvider.Instant();
            }

            exchange.Metrics.TotalSent += counterStream.TotalWritten; 
            
            var totalBodySize  = await
                exchange.Request.Body.CopyAndReturnCopied(exchange.UpStream, 1024 * 8,
                    (_) => { }, cancellationToken).ConfigureAwait(false);

            exchange.Metrics.TotalSent += totalBodySize;
            
            


            _parser.Read()


                
            // Read header from server 

            // Remove non forwardable headers 

            // Stream response 
        }
    }
}