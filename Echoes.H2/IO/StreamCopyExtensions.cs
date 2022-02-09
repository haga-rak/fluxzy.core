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

    public class Http11Utils
    {
        private readonly TunnelSetting _tunnelSetting;
        private readonly ITimingProvider _timingProvider;

        public Http11Utils(TunnelSetting tunnelSetting, ITimingProvider timingProvider)
        {
            _tunnelSetting = tunnelSetting;
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