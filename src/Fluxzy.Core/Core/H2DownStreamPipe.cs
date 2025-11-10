// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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

namespace Fluxzy.Core
{
    internal class H2DownStreamPipe : IDownStreamPipe
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;
        private readonly IIdProvider _idProvider;
        private readonly IExchangeContextBuilder _contextBuilder;

        private Task? _readLoop;
        private Task? _writeLoop;

        private readonly Channel<Exchange> _exchangeChannel = 
            Channel.CreateUnbounded<Exchange>();

        private readonly Channel<ReadOnlyMemory<byte>> _writeChannel =
                        Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
        
        private readonly Dictionary<int, ServerStreamWorker> _currentStreams = new();
        private readonly HeaderEncoder _headerEncoder;
        private readonly H2StreamSetting _h2StreamSetting = new H2StreamSetting();
        private readonly WindowSizeHolder _overallWindowSizeHolder;
        private readonly H2Logger _logger;
        private readonly CancellationToken _mainLoopToken;
        private readonly CancellationTokenSource _mainLoopTokenSource;

        private int _unNotifiedWindowSize;
        private bool _readHalted;
        private bool _writeHalted;
        private int _lastStreamId = int.MaxValue;
        private bool _disposed;
        private bool _goAwayReceived;
        private H2ErrorCode _goAwayErrorCode;

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
            _mainLoopTokenSource = new CancellationTokenSource();
            _mainLoopToken = _mainLoopTokenSource.Token;
        }

        public Authority RequestedAuthority { get; }

        public bool TunnelOnly { get; set; }

        public async Task Init(RsBuffer buffer)
        {
            // Make announcement to the client

            var prefaceMemory = buffer.Memory.Slice(0, H2Constants.Preface.Length);

            await _readStream.ReadExactAsync(prefaceMemory, _mainLoopToken);

            if (!prefaceMemory.Span.SequenceEqual(H2Constants.Preface)) {
                throw new FluxzyException("Invalid preface received");
            }

            _readLoop = ReadLoop(_mainLoopToken);
            _writeLoop = WriteLoop(_mainLoopToken);

            // validate announcement 
            // adjust settings 
        }

        private async ValueTask WriteRstStream(int streamIdentifier, H2ErrorCode errorCode, CancellationToken token)
        {
            var buffer = new byte[9 + 4];
            _ = new RstStreamFrame(streamIdentifier, errorCode).Write(buffer);
            await _writeChannel.Writer.WriteAsync(buffer, token);
        }

        private async ValueTask WriteAck(CancellationToken token)
        {
            await _writeChannel.Writer.WriteAsync(H2Helper.SettingAckBuffer, token);
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
            var buffer = new byte[9 + 4];
            var writtenLength = new WindowUpdateFrame(streamIdentifier, length).Write(buffer);
            await _writeChannel.Writer.WriteAsync(buffer.AsMemory().Slice(0, writtenLength), token);
        }

        private void OnGoAwayReceived(int lastStreamId, H2ErrorCode errorCode)
        {
            _goAwayReceived = true;
            _goAwayErrorCode = errorCode;
        }

        private void CheckoutServerStreamWorker(ServerStreamWorker streamWorker)
        {
            _currentStreams.Remove(streamWorker.StreamIdentifier);
            streamWorker.Dispose();
        }

        private async Task ReadLoop(CancellationToken token)
        {
            try {
                using var readBuffer = RsBuffer.Allocate(_h2StreamSetting.MaxFrameSizeAllowed + 9);

                while (!token.IsCancellationRequested) {

                    H2FrameReadResult frame;

                    try {
                        frame =
                            await H2FrameReader.ReadNextFrameAsync(_readStream, readBuffer.Memory,
                                token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        break;
                    }

                    if (frame.BodyType == H2FrameType.Settings) {
                        var sendAck = H2Helper.ProcessSettingFrame(_h2StreamSetting, frame);

                        if (sendAck)
                        {
                            await WriteAck(token);
                        }

                        continue;
                    }

                    if (frame.BodyType == H2FrameType.Goaway) {
                        frame.GetGoAwayFrame().Read(out var errorCode, out _lastStreamId);
                        OnGoAwayReceived(_lastStreamId, errorCode);
                        break;
                    }

                    if (frame.BodyType == H2FrameType.Priority) {
                        // IGNORED
                        continue;
                    }

                    // 

                    if (!_currentStreams.TryGetValue(frame.StreamIdentifier, out var worker)) {
                        worker = new ServerStreamWorker(frame.StreamIdentifier, _headerEncoder,
                            _overallWindowSizeHolder, _h2StreamSetting, _logger);

                        _currentStreams.Add(frame.StreamIdentifier, worker);
                    }

                    if (frame.BodyType == H2FrameType.PushPromise) {
                        var errorCode = H2ErrorCode.ProtocolError;
                        await WriteRstStream(frame.StreamIdentifier, errorCode, token);
                        CheckoutServerStreamWorker(worker);
                    }

                    if (frame.BodyType == H2FrameType.Headers) {
                        var errorCode = worker.ProcessHeaderFrame(ref frame);

                        if (errorCode != H2ErrorCode.NoError)
                        {
                            await WriteRstStream(frame.StreamIdentifier, errorCode, token);
                            CheckoutServerStreamWorker(worker);
                        }
                    }

                    if (frame.BodyType == H2FrameType.Continuation) {
                        var errorCode = worker.ProcessContinuation(ref frame);

                        if (errorCode != H2ErrorCode.NoError)
                        {
                            await WriteRstStream(frame.StreamIdentifier, errorCode, token);
                            CheckoutServerStreamWorker(worker);
                        }
                    }

                    if (frame.BodyType == H2FrameType.Data) {
                        var (errorCode, bodyLength, notifiableLength) =
                            await worker.ReceiveBodyFragment(frame, readBuffer, token);

                        if (errorCode != H2ErrorCode.NoError)
                        {
                            await WriteRstStream(frame.StreamIdentifier, errorCode, token);
                            CheckoutServerStreamWorker(worker);
                        }
                        else {
                            // send widow size increment stream level
                            if (notifiableLength > 0) {
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

                        _exchangeChannel.Writer.TryWrite(exchange);
                    }
                }
            }
            catch (Exception ex) {

                throw;
            }
            finally  {
                _readHalted = true;
            }
        }

        private async Task WriteLoop(CancellationToken token)
        {
            try {

                while (!token.IsCancellationRequested && await _writeChannel.Reader.WaitToReadAsync(token)) {
                    while (_writeChannel.Reader.TryRead(out var buffer)) {
                        await _writeStream.WriteAsync(buffer, token);
                    }
                }
            }
            catch (Exception ex) {
                // stream is closed. 
                throw;
            }
            finally {
                _writeHalted = true;
            }
        }
        

        public async ValueTask<Exchange?> ReadNextExchange(RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken token)
        {
            if (_disposed || _goAwayReceived || _readHalted || _writeHalted)
                return null; 

            using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_mainLoopToken, token);
            var combinedToken = combinedTokenSource.Token;

            var exchange = await _exchangeChannel.Reader.ReadAsync(combinedToken);

            return exchange;
        }

        public async ValueTask WriteResponseHeader(
            ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, int streamIdentifier, CancellationToken token)
        {
            using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_mainLoopToken, token);
            var combinedToken = combinedTokenSource.Token;

            var downStreamIdentifier = streamIdentifier + 1;

            var endStream = responseHeader.HasResponseBody("GET", out _);

            var payload = _headerEncoder.Encode(
                new HeaderEncodingJob(responseHeader.GetHttp11Header(),
                    downStreamIdentifier, 0),
                buffer, endStream);

            await _writeChannel.Writer.WriteAsync(payload, combinedToken);
        }

        public async ValueTask WriteResponseBody(Stream responseBodyStream, 
            RsBuffer rsBuffer, bool chunked, int streamIdentifier, CancellationToken token)
        {
            // take care of window size
            if (!_currentStreams.TryGetValue(streamIdentifier, out var worker)) {

                // stream already closed
                throw new FluxzyException($"Invalid Local H2 stream : identifier {streamIdentifier}");
            }

            using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_mainLoopToken, token);
            var combinedToken = combinedTokenSource.Token;

            var sendStreamIdentifier = streamIdentifier + 1;

            var readBuffer = ArrayPool<byte>.Shared.Rent(_h2StreamSetting.MaxFrameSizeAllowed + 9);

            try {
                int read; 

                while ((read = await responseBodyStream
                           .ReadAsync(readBuffer.AsMemory().Slice(0, _h2StreamSetting.MaxFrameSizeAllowed), combinedToken)) > 0)
                {
                    int offset = 0;

                    while (read > 0)
                    {
                        var toWrite = await worker.BookWindowSize(read, combinedToken);

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
                    
                        await _writeChannel.Writer.WriteAsync(rsBuffer.Memory.Slice(0, bodySize + 9), combinedToken);

                        offset += bodySize;
                        read -= bodySize;
                    }
                }

                // Send end stream
                new DataFrame(HeaderFlags.EndStream, 0, sendStreamIdentifier)
                    .WriteHeaderOnly(rsBuffer.Memory.Span, 0);

                await _writeChannel.Writer.WriteAsync(rsBuffer.Memory.Slice(0, 9), combinedToken);

            }
            finally {
                ArrayPool<byte>.Shared.Return(readBuffer);
            }
        }

        public (Stream ReadStream, Stream WriteStream) AbandonPipe()
        {
            return (_readStream, _writeStream);
        }

        public bool CanWrite => !_writeHalted;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _mainLoopTokenSource.Cancel();
            _writeChannel.Writer.TryComplete();
            _exchangeChannel.Writer.TryComplete();

            foreach (var (_, worker) in _currentStreams)
            {
                worker.Dispose();
            }

            _overallWindowSizeHolder.Dispose();
        }
    }
}
