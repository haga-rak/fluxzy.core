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
using YamlDotNet.Serialization;

namespace Fluxzy.Core
{
    internal class H2DownStreamPipe : IDownStreamPipe, IExitStream
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;
        private readonly IIdProvider _idProvider;
        private readonly IExchangeContextBuilder _contextBuilder;

        private Task? _readLoop;
        private Task _writeLoop;

        private readonly Channel<Exchange> _exchangeChannel = Channel.CreateUnbounded<Exchange>();
        private readonly RsBuffer _readBuffer = RsBuffer.Allocate(8 * 1024); 

        private H2StreamSetting _h2StreamSetting = new H2StreamSetting();

        /// <summary>
        /// TODO setup pipe options here
        /// </summary>
        private readonly Pipe _outControlPipe = new Pipe();

        private readonly Dictionary<int, ServerStreamWorker> _currentStreams = new();
        private readonly HeaderEncoder _headerEncoder;
        private int _lastStreamId = int.MaxValue;

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
            RstStreamFrame rstFrame = new RstStreamFrame(streamIdentifier, errorCode);
            Span<byte> buffer = stackalloc byte[rstFrame.BodyLength + 9];

            int length = rstFrame.Write(buffer);

            _outControlPipe.Writer.Write(buffer.Slice(0, length));
        }

        private async Task ReadLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested) {
                H2FrameReadResult frame =
                    await H2FrameReader.ReadNextFrameAsync(_readStream, _readBuffer.Memory,
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
                    worker = new ServerStreamWorker(frame.StreamIdentifier,
                        _h2StreamSetting.MaxHeaderSize, _headerEncoder);
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
                    var errorCode = await worker.ProcessData(frame, _readBuffer, token);
                    if (errorCode != H2ErrorCode.NoError)
                    {
                        WriteRstStream(frame.StreamIdentifier, errorCode);
                        _currentStreams.Remove(frame.StreamIdentifier);
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
            _readBuffer.Dispose();
        }

    }
}
