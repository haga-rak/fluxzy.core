// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using YamlDotNet.Core.Tokens;

namespace Fluxzy.Core
{
    internal class H2DownStreamPipe : IDownStreamPipe, IExitStream
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;
        private readonly IIdProvider _idProvider;
        private readonly IExchangeContextBuilder _contextBuilder;

        private Task? _readLoop;
        private Task? _writeLoop;

        private readonly Channel<Exchange> _exchangeChannel = Channel.CreateUnbounded<Exchange>();

        private readonly H2StreamSetting _h2StreamSetting = new H2StreamSetting();

        /// <summary>
        /// TODO setup pipe options here
        /// </summary>
        private readonly Pipe _outControlPipe = new Pipe();

        private readonly Dictionary<int, ServerStreamWorker> _currentStreams = new();
        private readonly HeaderEncoder _headerEncoder;
        private int _lastStreamId = int.MaxValue;

        private readonly WindowSizeHolder _overallWindowSizeHolder;
        private readonly H2Logger _logger;

        private int _unNotifiedWindowSize;


        public H2DownStreamPipe(Authority requestedAuthority, Stream readStream, Stream writeStream,
            IIdProvider idProvider,
            IExchangeContextBuilder contextBuilder)
        {
            _readStream = readStream;
            _writeStream = writeStream;
            _idProvider = idProvider;
            _contextBuilder = contextBuilder;
            RequestedAuthority = requestedAuthority;

            var hPackEncoder =
                new HPackEncoder(new EncodingContext(ArrayPoolMemoryProvider<char>.Default));

            var hPackDecoder =
                new HPackDecoder(new DecodingContext(RequestedAuthority, 
                    ArrayPoolMemoryProvider<char>.Default));

            
            _headerEncoder = new HeaderEncoder(hPackEncoder, hPackDecoder, _h2StreamSetting);
            _logger = new H2Logger(requestedAuthority, -1);
            _overallWindowSizeHolder = new WindowSizeHolder(_logger, _h2StreamSetting.OverallWindowSize, 0);

        }

        public async Task Init(RsBuffer buffer, CancellationToken token)
        {
            // Make announcement to the client

            var prefaceMemory = buffer.Memory.Slice(0, H2Constants.Preface.Length);

            await _readStream.ReadExactAsync(prefaceMemory, token);

            if (!prefaceMemory.Span.SequenceEqual(H2Constants.Preface)) {
                throw new FluxzyException("Invalid preface received");
            }

            _readLoop = ReadLoop(token);
            _writeLoop = WriteLoop(token);

            // validate announcement 

            // adjust settings 
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            _outControlPipe.Writer.Write(buffer);
        }

        private void WriteRstStream(int streamIdentifier, H2ErrorCode errorCode)
        {
            var rstFrame = new RstStreamFrame(streamIdentifier, errorCode);
            Span<byte> buffer = stackalloc byte[rstFrame.BodyLength + 9];

            int length = rstFrame.Write(buffer);

            _outControlPipe.Writer.Write(buffer.Slice(0, length));
        }

        private async ValueTask NotifyConnectionWindowSizeDecrement(int length, CancellationToken token)
        {
            _unNotifiedWindowSize += length;

            if (_unNotifiedWindowSize > (_h2StreamSetting.Local.WindowSize / 2)) {

                await SendWindowUpdateFrame(0, _unNotifiedWindowSize, token);
                _unNotifiedWindowSize = 0;
            }
        }

        private async ValueTask SendWindowUpdateFrame(int streamIdentifier, int length, CancellationToken token)
        {
            using var buffer = RsBuffer.Allocate(9 + 4);

            var writtenLength = new WindowUpdateFrame(streamIdentifier, length).Write(buffer.Buffer);
            await _outControlPipe.Writer.WriteAsync(buffer.Memory.Slice(0, writtenLength), token);
        }


        private async Task ReadLoop(CancellationToken token)
        {
            using var readBuffer = RsBuffer.Allocate(_h2StreamSetting.MaxFrameSizeAllowed + 9);
           
            while (!token.IsCancellationRequested) {
                H2FrameReadResult frame =
                    await H2FrameReader.ReadNextFrameAsync(_readStream, readBuffer.Memory,
                        token).ConfigureAwait(false);

                if (frame.BodyType == H2FrameType.Settings)
                {
                    H2Helper.ProcessSettingFrame(_h2StreamSetting, frame, this);
                    continue;
                }
                
                if (frame.BodyType == H2FrameType.Goaway) {
                    frame.GetGoAwayFrame().Read(out var errorCode, out _lastStreamId);
                    break;
                }

                if (frame.BodyType == H2FrameType.Priority) {
                    // IGNORED
                    continue;
                }

                // 

                if (!_currentStreams.TryGetValue(frame.StreamIdentifier, out var worker))
                {
                    worker = new ServerStreamWorker(frame.StreamIdentifier, _headerEncoder,
                        _overallWindowSizeHolder, _h2StreamSetting, _logger);
                    _currentStreams.Add(frame.StreamIdentifier, worker);
                }

                if (frame.BodyType == H2FrameType.PushPromise) {
                    var errorCode = H2ErrorCode.ProtocolError;
                    WriteRstStream(frame.StreamIdentifier, errorCode);
                    _currentStreams.Remove(frame.StreamIdentifier);
                }

                if (frame.BodyType == H2FrameType.Headers) {
                    var errorCode = worker.ProcessHeaderFrame(ref frame);

                    if (errorCode != H2ErrorCode.NoError) {
                        WriteRstStream(frame.StreamIdentifier, errorCode);
                        _currentStreams.Remove(frame.StreamIdentifier);
                    }
                }

                if (frame.BodyType == H2FrameType.Continuation) {
                    var errorCode = worker.ProcessContinuation(ref frame);

                    if (errorCode != H2ErrorCode.NoError) {
                        WriteRstStream(frame.StreamIdentifier, errorCode);
                        _currentStreams.Remove(frame.StreamIdentifier);
                    }
                }

                if (frame.BodyType == H2FrameType.Data) {
                    var (errorCode, bodyLength, notifiableLength) = await worker.ReceiveBodyFragment(frame, readBuffer, token);

                    if (errorCode != H2ErrorCode.NoError)
                    {
                        WriteRstStream(frame.StreamIdentifier, errorCode);
                        _currentStreams.Remove(frame.StreamIdentifier);
                    }
                    else {
                        // send widow size increment stream level
                        if (notifiableLength > 0)
                        {
                            await SendWindowUpdateFrame(frame.StreamIdentifier, notifiableLength.Value,
                                token);
                        }

                        // send window size increment connection level
                        if (bodyLength > 0) {
                            await NotifyConnectionWindowSizeDecrement(bodyLength, token);
                        }
                    }
                }

                if (worker.ReadyToCreateExchange) {
                    var exchange = await worker.CreateExchange(_idProvider, _contextBuilder,
                        RequestedAuthority, true);

                    await _exchangeChannel.Writer.WriteAsync(exchange, token);
                }
            }
        }

        private async Task WriteLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var result = await _outControlPipe.Reader.ReadAsync(token);
                var buffer = result.Buffer;

                foreach (var segment in buffer)
                {
                    await _writeStream.WriteAsync(segment, token);
                }

                _outControlPipe.Reader.AdvanceTo(buffer.End);
            }
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
            ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, int streamIdentifier, CancellationToken token)
        {
            // determine which stream identifier? 
            // encode headers
            // packetize 
            // send

            var downStreamIdentifier = streamIdentifier + 1;

            var endStream = responseHeader.HasResponseBody("GET", out _);

            var payload = _headerEncoder.Encode(
                new HeaderEncodingJob(responseHeader.GetHttp11Header(),
                    downStreamIdentifier, 0),
                buffer, endStream);

            _outControlPipe.Writer.Write(payload.Span);

            return ValueTask.CompletedTask;
        }

        public async ValueTask WriteResponseBody(Stream responseBodyStream, 
            RsBuffer rsBuffer, bool chunked, int streamIdentifier, CancellationToken token)
        {
            // take care of window size
            if (!_currentStreams.TryGetValue(streamIdentifier, out var worker)) {

                // stream already closed
                throw new FluxzyException($"Invalid Local H2 stream : identifier {streamIdentifier}");
            }

            var sendStreamIdentifier = streamIdentifier + 1;

            var readBuffer = ArrayPool<byte>.Shared.Rent(_h2StreamSetting.MaxFrameSizeAllowed + 9);

            try {
                int read; 

                while ((read = await responseBodyStream
                           .ReadAsync(readBuffer.AsMemory().Slice(0, _h2StreamSetting.MaxFrameSizeAllowed), token)) > 0)
                {
                    int offset = 0;

                    while (read > 0)
                    {
                        var toWrite = await worker.BookWindowSize(read, token);

                        if (toWrite == 0)
                        {
                            // stream closed 
                            return;
                        }

                        var bodySize = Math.Min(toWrite, _h2StreamSetting.MaxFrameSizeAllowed);
                        var writable = readBuffer.AsMemory().Slice(offset, bodySize);

                        rsBuffer.Ensure(bodySize + 9);

                        new DataFrame(HeaderFlags.None, bodySize, sendStreamIdentifier)
                            .WriteHeaderOnly(rsBuffer.Memory.Span, bodySize);

                        writable
                            .Slice(0, bodySize).CopyTo(rsBuffer.Memory.Slice(9, bodySize));
                    
                        await _outControlPipe.Writer.WriteAsync(rsBuffer.Memory.Slice(0, bodySize + 9), token);

                        offset += bodySize;
                        read -= bodySize;
                    }
                }

                // Send end stream
                new DataFrame(HeaderFlags.EndStream, 0, sendStreamIdentifier)
                    .WriteHeaderOnly(rsBuffer.Memory.Span, 0);

                await _outControlPipe.Writer.WriteAsync(rsBuffer.Memory.Slice(0, 9), token);

            }
            finally {
                ArrayPool<byte>.Shared.Return(readBuffer);
            }
        }

        public (Stream ReadStream, Stream WriteStream) AbandonPipe()
        {
            throw new System.NotImplementedException();
        }

        public bool CanWrite { get; }

        public void Dispose()
        {

        }

    }
}
