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
        public static async Task<int> ReadHeaderBlock(Stream input, Memory<byte> buffer, CancellationToken token)
        {
            Memory<char> headerData = new Memory<char>(new char[MaxHeaderSize]); 

            while (true)
            {
                var readen = await input.ReadAsync(buffer, token);

                

                // Check for CRLF

            }


            
        }
    }
}