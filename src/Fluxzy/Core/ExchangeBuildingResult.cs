// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Threading;
using Fluxzy.Clients;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    public class ExchangeBuildingResult : ILocalLink
    {
        private static int _count;

        public ExchangeBuildingResult(
            Authority authority,
            Stream readStream,
            Stream writeStream,
            Exchange provisionalExchange, bool tunnelOnly)
        {
            Id = Interlocked.Increment(ref _count);
            Authority = authority;
            ReadStream = readStream;
            WriteStream = writeStream;
            ProvisionalExchange = provisionalExchange;
            TunnelOnly = tunnelOnly;

            if (DebugContext.EnableNetworkFileDump)
            {
                ReadStream = new DebugFileStream($"raw/{Id:0000}_browser_",
                    ReadStream, true);

                WriteStream = new DebugFileStream($"raw/{Id:0000}_browser_",
                    WriteStream, false);
            }
        }

        public int Id { get; }

        public Authority Authority { get; }

        public Exchange ProvisionalExchange { get; }

        public bool TunnelOnly { get; }

        public Stream ReadStream { get; }

        public Stream WriteStream { get; }
    }
}
