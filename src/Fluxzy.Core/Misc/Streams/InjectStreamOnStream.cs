// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;

namespace Fluxzy.Misc.Streams
{
    public class InjectStreamOnStream : Stream
    {
        // The matcher 
        private readonly IBinaryMatcher _binaryMatcher;

        // The stream to be injected
        private readonly Stream _injectedStream;

        // The original stream 
        private readonly Stream _innerStream;

        // The pattern to match
        private readonly byte[] _matchingPattern;

        // Previous read that was not validated yet
        private readonly byte[] _pendingUnvalidatedBuffer;

        // Validated buffer that waits to be read 
        private readonly byte[] _pendingValidatedBuffer;

        // Flag indicating that injection should start
        private bool _continueInjecting;

        // Flag indicating that the injected stream has reached EOF
        private bool _injectedStreamEof;

        private bool _innerStreamEof;

        // Unvalidated buffer length
        private int _pendingUnvalidatedBufferLength;

        // Validated buffer length
        private int _pendingValidatedBufferLength;

        private readonly int _unvalidatedBufferLength;

        public InjectStreamOnStream(
            Stream innerStream,
            IBinaryMatcher binaryMatcher,
            byte[] matchingPattern,
            Stream injectedStream, int unvalidatedBufferLength = 512)
        {
            _innerStream = innerStream;
            _binaryMatcher = binaryMatcher;
            _matchingPattern = matchingPattern;
            _injectedStream = injectedStream;
            _unvalidatedBufferLength = unvalidatedBufferLength;

            var bufferSize = Math.Max(_matchingPattern.Length * 2, 4096);

            _pendingValidatedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
            _pendingUnvalidatedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
        }

        public override bool CanRead { get; } = true;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = false;

        public override long Length => throw new NotSupportedException();

        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            while (true) {
                // Copy pendingBuffer to reader 

                if (_pendingValidatedBufferLength > 0) {
                    var readable = Math.Min(count, _pendingValidatedBufferLength);
                    _pendingValidatedBuffer.AsSpan(0, readable).CopyTo(buffer.AsSpan(offset));

                    // Shift buffer 

                    BufferArrayShiftUtilities.ShiftOffsetToZero(_pendingValidatedBuffer, readable,
                        _pendingValidatedBufferLength - readable);

                    _pendingValidatedBufferLength -= readable;

                    return readable;
                }

                // Drain zone copy to the reader

                if (_continueInjecting && !_injectedStreamEof) {
                    var injectRead = _injectedStream.Read(buffer, offset, count);
                    _continueInjecting = injectRead > 0;

                    if (injectRead > 0) {
                        return injectRead;
                    }
                    else {
                        _injectedStreamEof = true;
                    }
                }

                if (_innerStreamEof) {
                    break;
                }
                
                var canBeStored =
                    _pendingUnvalidatedBuffer.Length - _pendingUnvalidatedBufferLength;

                var maxRead = Math.Min(buffer.Length, canBeStored); // MAYBE not usefull ? 

                var read = _innerStream.Read(_pendingUnvalidatedBuffer,
                    _pendingUnvalidatedBufferLength, maxRead);

                _pendingUnvalidatedBufferLength += read;

                if (read == 0) {
                    _innerStreamEof = true;

                    if (_pendingUnvalidatedBufferLength == 0) {
                        // EOF 
                        return 0;
                    }

                    Array.Copy(_pendingUnvalidatedBuffer, 0,
                        _pendingValidatedBuffer, 0, _pendingUnvalidatedBufferLength);

                    _pendingValidatedBufferLength = _pendingUnvalidatedBufferLength;

                    continue;
                }

                var (index, length, shiftLength) = _binaryMatcher
                    .FindIndex(_pendingUnvalidatedBuffer.AsSpan(0, _pendingUnvalidatedBufferLength),
                        _matchingPattern);

                if (index < 0) {
                    var unvalidatedLength = _matchingPattern.Length + _unvalidatedBufferLength;

                    var validatedLength = _pendingUnvalidatedBufferLength - unvalidatedLength;

                    if (validatedLength > 0) {
                        _pendingUnvalidatedBuffer.AsSpan(0, validatedLength).CopyTo(_pendingValidatedBuffer);

                        BufferArrayShiftUtilities.ShiftOffsetToZero(_pendingUnvalidatedBuffer,
                            validatedLength, unvalidatedLength);

                        _pendingUnvalidatedBufferLength -= validatedLength;
                        _pendingValidatedBufferLength = validatedLength;
                    }
                }
                else {
                    // Copy to validated buffer 

                    var originalLength = index + length;
                    var validatedLength = index + shiftLength;

                    _pendingUnvalidatedBuffer.AsSpan(0, validatedLength).CopyTo(_pendingValidatedBuffer);

                    BufferArrayShiftUtilities.ShiftOffsetToZero(_pendingUnvalidatedBuffer,
                        originalLength, _pendingUnvalidatedBufferLength - originalLength);

                    _pendingUnvalidatedBufferLength -= originalLength;
                    _pendingValidatedBufferLength = validatedLength;

                    // Copy to reader 

                    _continueInjecting = true;
                }
            }

            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Readonly Stream");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Readonly Stream");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Readonly Stream");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                _injectedStream.Dispose();
                _innerStream.Dispose();
                ArrayPool<byte>.Shared.Return(_pendingValidatedBuffer);
                ArrayPool<byte>.Shared.Return(_pendingUnvalidatedBuffer);
            }

            base.Dispose(disposing);
        }
    }

    internal static class BufferArrayShiftUtilities
    {
        public static void ShiftOffsetToZero(byte[] buffer, int offset, int length)
        {
            Array.Copy(buffer, offset, buffer, 0, length);
        }
    }
}
