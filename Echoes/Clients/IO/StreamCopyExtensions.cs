﻿// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.IO
{
    public static class StreamCopyExtensions
    {
        public static async ValueTask<long> CopyDetailed(this Stream source,
            Stream destination,
            int bufferSize, Action<int> onContentCopied, CancellationToken cancellationToken)
        {
            return await CopyDetailed(source, destination, new byte[bufferSize], onContentCopied,
                cancellationToken);
        }

        public static async ValueTask<int> Drain(this Stream stream, int bufferSize = 16 * 1024)
        {
            var buffer = new byte[bufferSize];
            int read; 
            var total = 0; 

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                total += read; 
            }

            return total; 
        }


        public static async ValueTask<long> CopyDetailed(this Stream source,
            Stream destination,
            byte [] buffer, Action<int> onContentCopied, CancellationToken cancellationToken)
        {
            long totalCopied = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                       .ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                onContentCopied(read);

                await destination.FlushAsync(cancellationToken);

                totalCopied += read;
            }

            return totalCopied;
        }
    }

    public class Http11Utils
    {
        private readonly ClientSetting _clientSetting;
        private readonly ITimingProvider _timingProvider;

        public Http11Utils(ClientSetting clientSetting, ITimingProvider timingProvider)
        {
            _clientSetting = clientSetting;
            _timingProvider = timingProvider;
        }

    }


    public readonly struct HeaderBlockReadResult
    {
        public HeaderBlockReadResult(int headerLength, int totalReadLength)
        {
            HeaderLength = headerLength;
            TotalReadLength = totalReadLength;
        }

        public int HeaderLength { get;  }

        public int TotalReadLength { get;  }
    }
}