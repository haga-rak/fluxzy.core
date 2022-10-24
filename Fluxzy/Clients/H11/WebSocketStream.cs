// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Clients.H11
{
    /// <summary>
    /// A websocket reading stream 
    /// </summary>
    public class WebSocketStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly Action<WsMessage> _onMessage;
        private readonly Func<Stream> _outStream;
        private readonly Pipe _pipe;
        private readonly Task _runningTask;

        private WsMessage? _currentMessage;
        private int _messageCounter;
        private readonly int _maxBufferedWsMessage = 1024; 

        public WebSocketStream(Stream innerStream,
            Action<WsMessage> onMessage, Func<Stream> outStream) 
        {
            _innerStream = innerStream;
            _onMessage = onMessage;
            _outStream = outStream;
            _pipe = new Pipe();
            _runningTask = InitRead();
            
        }

        private async Task InitRead()
        {
            while (true)
            {
                if (!_pipe.Reader.TryRead(out var readResult)) {
                    readResult = await _pipe.Reader.ReadAsync();
                }

                if (readResult.IsCompleted || readResult.IsCanceled)
                    return;

                var buffer = readResult.Buffer;
                var headerLength = -1;

                WsFrame wsFrame = default; 

                if (!((headerLength = TryReadWsFrameHeader(ref buffer, ref wsFrame)) < 0)) {

                    // not enough data to complete the header frame send back to read 

                    _pipe.Reader.AdvanceTo(buffer.Start); 
                    continue; 
                }

                _pipe.Reader.AdvanceTo(buffer.GetPosition(headerLength));

                _currentMessage ??= new WsMessage(++_messageCounter);

                await _currentMessage
                    .AddFrame(wsFrame, _maxBufferedWsMessage,
                        _pipe.Reader, _outStream);

                if (wsFrame.FinalFragment) {
                    _onMessage(_currentMessage); 
                    _currentMessage = null;
                }
            }
        }
        
        private int TryReadWsFrameHeader(ref ReadOnlySequence<byte> sequencBuffer, ref WsFrame wsFrame)
        {
            if (sequencBuffer.Length < 2)
                return -1;

            var buffer = sequencBuffer.FirstSpan;

            wsFrame.FinalFragment = (buffer[0] & 0x80) > 0;

            wsFrame.OpCode = (WsOpCode) (buffer[0] & 0xF);

            int byteIndex = 1;

            var maskedPayload = (buffer[byteIndex] & 0x80) > 0;
            var payloadIndicator = (byte) (buffer[byteIndex] & 0X7f);

            if (payloadIndicator < 126) {
                wsFrame.PayloadLength = payloadIndicator;
            }
            else
            {
                var startBuffer = buffer.Slice(byteIndex);

                if (payloadIndicator == 126) {
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

                byteIndex += 1;
            }


            if (maskedPayload) {

                var startBuffer = buffer.Slice(byteIndex);

                if (startBuffer.Length < 4)
                    return -1;

                wsFrame.PayloadLength = BinaryPrimitives.ReadInt32BigEndian(startBuffer);
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
            _pipe.Writer.Write(new ReadOnlySpan<byte>(buffer).Slice(offset, count));
            return _innerStream.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            _pipe.Writer.Write(buffer.Span);
            return base.ReadAsync(buffer, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
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

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false; 
        public override long Length => throw new InvalidOperationException("Stream is non seekable");
        public override long Position { get; set; }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _pipe.Writer.Complete(); 
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            await _runningTask;
        }
    }
}