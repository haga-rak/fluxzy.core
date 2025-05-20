// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    public class H2DownStreamPipe : IDownStreamPipe
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;
        private Task _readLoop;

        private Channel<Exchange> _exchangeChannel = Channel.CreateUnbounded<Exchange>();

        public H2DownStreamPipe(Authority requestedAuthority, Stream readStream, Stream writeStream)
        {
            _readStream = readStream;
            _writeStream = writeStream;
            RequestedAuthority = requestedAuthority;
        }

        public async Task Init(RsBuffer buffer, CancellationToken token)
        {
            // Make announcement to the client

            var prefaceMemory = buffer.Memory.Slice(0, H2Constants.Preface.Length);

            await _readStream.ReadExactAsync(prefaceMemory, token);

            if (!prefaceMemory.Span.SequenceEqual(H2Constants.Preface)) {
                throw new InvalidOperationException("Invalid preface");
            }

            _readLoop = ReadLoop(); 

            // validate announcement 

            // adjust settings 
        }


        private async Task ReadLoop()
        {

        }



        public Authority RequestedAuthority { get; }

        public bool TunnelOnly { get; set; }

        public async ValueTask<Exchange?> ReadNextExchange(RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken token)
        {
            // RECEIVE REQUEST IN A STREAM LOOP 
            // RECEIVE BODY PROMISE (probably on a PipeStream) 
            // RETURN AN EXCHANGE 
            // SAVE STREAM INDEX 

            var exchange = await _exchangeChannel.Reader.ReadAsync(token);

            return exchange;
        }

        public ValueTask WriteResponseHeader(
            ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask WriteResponseBody(Stream responseBodyStream, RsBuffer rsBuffer, bool chunked, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public (Stream ReadStream, Stream WriteStream) AbandonPipe()
        {
            throw new System.NotImplementedException();
        }

        public bool CanWrite { get; }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

    }
}
