// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Core
{
    internal class ServerStreamWorker
    {
        private readonly int _streamIdentifier;
        private readonly IHeaderEncoder _headerEncoder;
        private readonly byte[] _headerBuffer;
        private int _receivedHeaderLength;
        private bool _endHeader;
        private bool _endStream;
        private bool _exchangeCreated;

        private Pipe? _requestBodyPipe;
        
        public ServerStreamWorker(
            int streamIdentifier,
            int maxHeaderSize,
            IHeaderEncoder headerEncoder)
        {
            _streamIdentifier = streamIdentifier;
            _headerEncoder = headerEncoder;
            _headerBuffer = new byte[maxHeaderSize];
        }

        /// <summary>
        /// returns false if stream shall be go awayed
        /// </summary>
        /// <param name="headerFrame"></param>
        /// <returns></returns>
        public H2ErrorCode ProcessHeaderFrame(ref H2FrameReadResult headerFrame)
        {
            var frame = headerFrame.GetHeadersFrame();

            if (frame.EndStream) {
                _endStream = true;
            }

            return ReceiveHeaderFragment(frame.Data.Span, frame.EndHeaders);
        }

        private H2ErrorCode ReceiveHeaderFragment(ReadOnlySpan<byte> data, bool endHeaders)
        {
            var futureHeaderLength = _receivedHeaderLength + data.Length;

            if (futureHeaderLength > _headerBuffer.Length)
                return H2ErrorCode.FrameSizeError;

            data.CopyTo(_headerBuffer.AsSpan(_receivedHeaderLength));

            _receivedHeaderLength = futureHeaderLength;

            if (endHeaders) {
                _endHeader = true;
            }

            return H2ErrorCode.NoError;
        }

        public H2ErrorCode ProcessContinuation(ref H2FrameReadResult headerFrame)
        {
            var frame = headerFrame.GetContinuationFrame(); 
            return ReceiveHeaderFragment(frame.Data.Span, frame.EndHeaders);
        }

        public bool ReadyToCreateExchange => _endHeader && !_exchangeCreated;

        public async Task<Exchange> CreateExchange(
            IIdProvider idProvider,
            IExchangeContextBuilder contextBuilder, 
            Authority authority, bool secure)

        {
            _exchangeCreated = true; 

            var plainRequest =
                H2Helper.DecodeAndAllocate(_headerEncoder, _headerBuffer.AsSpan(0, _receivedHeaderLength));
            var receivedFromProxy = ITimingProvider.Default.Instant();

            var requestHeader = new RequestHeader(plainRequest, true);

            Stream bodyStream;

            if (_endStream) {
                bodyStream = Stream.Null; // no response body
            }
            else {
                _requestBodyPipe = new Pipe(); // TODO configure pipe settings
                bodyStream = _requestBodyPipe.Reader.AsStream();
            }

            var context = await contextBuilder.Create(authority, secure);

            var exchange = new Exchange(idProvider, context, authority, requestHeader, bodyStream, "h2",
                receivedFromProxy);

            return exchange;
        }
    }
}
