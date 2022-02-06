// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.IO
{
    public static class StreamCopyExtensions
    {

        public static async Task<long> CopyAndReturnCopied(this
                Stream source,
            Stream destination,
            int bufferSize, Action<int> onContentCopied, CancellationToken cancellationToken)
        {
            long totalCopied = 0;

            var buffer = new byte[bufferSize];
            int read;

            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                       .ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                onContentCopied(read);

                totalCopied += read;
            }

            return totalCopied;
        }
    }

    public static class Http11Utils
    {
        public static int MaxHeaderSize { get; set; } = 8192; 

        /// <summary>
        /// Read header block from input to buffer. Returns the total header length including double CRLF
        /// </summary>
        /// <param name="input"></param>
        /// <param name="buffer"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<ReadOnlyMemory<char>> ReadAndAllocateHeaderBlock(Stream input, Memory<byte> buffer, CancellationToken token)
        {
            var bufferIndex = buffer;
            var totalRead = 0;
            var indexFound = 0; 

            while (totalRead < buffer.Length)
            {
                var currentRead = await input.ReadAsync(bufferIndex, token);

                var start = totalRead - 4 < 0 ? 0 : (totalRead - 4);

                var searchBuffer = buffer.Slice(start, currentRead + (totalRead - start)); // We should look at that buffer 

                totalRead += currentRead;
                bufferIndex = bufferIndex.Slice(currentRead);

                var detected = DetectCrLf(searchBuffer);

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

            var memory = new Memory<char>(new char[indexFound]);
            
            System.Text.Encoding.ASCII.GetChars(buffer.Span.Slice(0, indexFound), memory.Span);

            return memory; 

        }

        private static int DetectCrLf(Memory<byte> buffer)
        {
            Span<char> charBuffer = stackalloc char[buffer.Length] ;
            System.Text.Encoding.ASCII.GetChars(buffer.Span, charBuffer);

            return charBuffer.IndexOf("\r\n\r\n".AsSpan()); 
        }
    }
}