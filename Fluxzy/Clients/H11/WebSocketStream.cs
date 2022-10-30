//// Copyright 2022 - Haga Rakotoharivelo

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Clients.H11
{
    /// <summary>
    ///     A websocket reading stream
    /// </summary>
    public class WebSocketStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly ITimingProvider _timingProvider;
        private readonly CancellationToken _token;
        private readonly Action<WsMessage> _onMessage;
        private readonly Func<int, Stream> _outStream;
        private readonly Pipe _pipe;
        private readonly Task _runningTask;
        private readonly int _maxBufferedWsMessage = 1024;

        // private WsMessage? _currentMessage;
        private int _messageCounter;

        private WsMessage? _current;

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new InvalidOperationException("Stream is non seekable");

        public override long Position { get; set; }

        public WebSocketStream(Stream innerStream, ITimingProvider timingProvider, CancellationToken token,
            Action<WsMessage> onMessage, Func<int, Stream> outStream)
        {
            _innerStream = innerStream;
            _timingProvider = timingProvider;
            _token = token;
            _onMessage = onMessage;
            _outStream = outStream;
            _pipe = new Pipe();
            _runningTask = InitRead();
        }

        private async Task InitRead()
        {
            while (true)
            {
                if (!_pipe.Reader.TryRead(out var readResult))
                    readResult = await _pipe.Reader.ReadAsync(_token);

                if (readResult.IsCompleted || readResult.IsCanceled)
                    return;

                var buffer = readResult.Buffer;
                var headerLength = -1;

                WsFrame wsFrame = default;

                if ((headerLength = TryReadWsFrameHeader(ref buffer, ref wsFrame)) < 0)
                {
                    // not enough data to complete the header frame send back to read 

                    _pipe.Reader.AdvanceTo(buffer.Start);

                    continue;
                }

                _pipe.Reader.AdvanceTo(buffer.GetPosition(headerLength));

                if (wsFrame.OpCode >= (WsOpCode)8)
                {
                    // Control frame

                    var immediateMessage = new WsMessage(++_messageCounter);
                    wsFrame.FinalFragment = true;
                    await immediateMessage.AddFrame(wsFrame, _maxBufferedWsMessage, _pipe.Reader, _outStream, _token);
                    immediateMessage.MessageEnd = _timingProvider.Instant();
                    _onMessage(immediateMessage);

                    continue;
                }

                _current ??= new WsMessage(++_messageCounter)
                {
                    MessageStart = _timingProvider.Instant()
                };

                await _current
                    .AddFrame(wsFrame, _maxBufferedWsMessage,
                        _pipe.Reader, _outStream, _token);

                if (wsFrame.FinalFragment)
                {
                    _current.MessageEnd = _timingProvider.Instant();
                    _onMessage(_current);
                    _current = null;
                }
            }
        }

        private int TryReadWsFrameHeader(ref ReadOnlySequence<byte> sequence, ref WsFrame wsFrame)
        {
            if (sequence.Length < 2)
                return -1;

            var buffer = sequence.FirstSpan;

            wsFrame.FinalFragment = (buffer[0] & 0x80) > 0;

            wsFrame.OpCode = (WsOpCode)(buffer[0] & 0xF);

            var byteIndex = 1;

            var maskedPayload = (buffer[byteIndex] & 0x80) > 0;
            var payloadIndicator = (byte)(buffer[byteIndex] & 0X7f);

            if (payloadIndicator < 126)
            {
                wsFrame.PayloadLength = payloadIndicator;
                byteIndex++;
            }
            else
            {
                byteIndex++;
                var startBuffer = buffer.Slice(byteIndex);

                if (payloadIndicator == 126)
                {
                    if (startBuffer.Length < 2)
                        return -1; // Not enough data 

                    wsFrame.PayloadLength = BinaryPrimitives.ReadUInt16BigEndian(startBuffer);
                    byteIndex += 2;
                }
                else
                {
                    if (startBuffer.Length < 4)
                        return -1; // Not enough data 

                    wsFrame.PayloadLength = BinaryPrimitives.ReadInt64BigEndian(startBuffer);
                    byteIndex += 8;
                }
            }

            if (maskedPayload)
            {
                var startBuffer = buffer.Slice(byteIndex);

                if (startBuffer.Length < 4)
                    return -1;

                wsFrame.MaskedPayload = BinaryPrimitives.ReadUInt32BigEndian(startBuffer);
                byteIndex += 4;
            }

            // Reading the buffer 

            return byteIndex;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readCount = _innerStream.Read(buffer, offset, count);
            _pipe.Writer.Write(new ReadOnlySpan<byte>(buffer).Slice(offset, readCount));

            return readCount;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            var read = await _innerStream.ReadAsync(buffer, cancellationToken);

            _pipe.Writer.Write(buffer.Span.Slice(0, read));
            await _pipe.Writer.FlushAsync(cancellationToken);

            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Stream is non seekable");
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Stream is non seekable");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Stream is non seekable");
        }

        protected override void Dispose(bool disposing)
        {
            _innerStream.Dispose();
            _pipe.Writer.Complete();
        }

        public override async ValueTask DisposeAsync()
        {
            await _innerStream.DisposeAsync();
            Dispose(true);
            await _runningTask;
        }
    }
}
