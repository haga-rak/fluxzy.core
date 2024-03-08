// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            for (;;) {
              
                if (_pendingValidatedBufferLength > 0) {
                    // Copy pendingBuffer to reader 
                    return ReadPendingValidatedBuffer(buffer.AsSpan(offset, count));
                }

                if (_continueInjecting && !_injectedStreamEof) {
                    // drain injected stream to the reader 

                    var injectRead = _injectedStream.Read(buffer, offset, count);
                    _continueInjecting = injectRead > 0;

                    if (injectRead > 0) {
                        return injectRead;
                    }

                    _injectedStreamEof = true;
                }

                if (_innerStreamEof) {
                    break;
                }

                if (_injectedStreamEof && _pendingUnvalidatedBufferLength == 0) {
                    // Avoid seeking index when buffering is done 

                    var directRead = _innerStream.Read(buffer,
                        offset, count);

                    return directRead; 
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

                ProcessPatternLookup();
            }

            return 0;
        }


        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            for (; ; )
            {

                if (_pendingValidatedBufferLength > 0)
                {
                    // Copy pendingBuffer to reader 
                    return ReadPendingValidatedBuffer(buffer.Span);
                }

                if (_continueInjecting && !_injectedStreamEof)
                {
                    // drain injected stream to the reader 

                    var injectRead = await _injectedStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    _continueInjecting = injectRead > 0;

                    if (injectRead > 0)
                    {
                        return injectRead;
                    }

                    _injectedStreamEof = true;
                }

                if (_innerStreamEof)
                {
                    break;
                }

                if (_injectedStreamEof && _pendingUnvalidatedBufferLength == 0)
                {
                    // Avoid seeking index when buffering is done 

                    var directRead = await _innerStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                    return directRead;
                }

                var canBeStored =
                    _pendingUnvalidatedBuffer.Length - _pendingUnvalidatedBufferLength;

                var maxRead = Math.Min(buffer.Length, canBeStored); // MAYBE not usefull ? 

                var read = await _innerStream.ReadAsync(_pendingUnvalidatedBuffer,
                    _pendingUnvalidatedBufferLength, maxRead, cancellationToken).ConfigureAwait(false);

                _pendingUnvalidatedBufferLength += read;

                if (read == 0)
                {
                    _innerStreamEof = true;

                    if (_pendingUnvalidatedBufferLength == 0)
                    {
                        // EOF 
                        return 0;
                    }

                    Array.Copy(_pendingUnvalidatedBuffer, 0,
                        _pendingValidatedBuffer, 0, _pendingUnvalidatedBufferLength);

                    _pendingValidatedBufferLength = _pendingUnvalidatedBufferLength;

                    continue;
                }

                ProcessPatternLookup();
            }

            return 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ReadPendingValidatedBuffer(Span<byte> buffer)
        {
            var readable = Math.Min(buffer.Length, _pendingValidatedBufferLength);
            _pendingValidatedBuffer.AsSpan(0, readable).CopyTo(buffer);

            // Shift buffer 

            BufferArrayShiftUtilities.ShiftOffsetToZero(_pendingValidatedBuffer, readable,
                _pendingValidatedBufferLength - readable);

            _pendingValidatedBufferLength -= readable;

            return readable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessPatternLookup()
        {
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
            if (disposing)
            {
                ArrayPool<byte>.Shared.Return(_pendingValidatedBuffer);
                ArrayPool<byte>.Shared.Return(_pendingUnvalidatedBuffer);
                _injectedStream.Dispose();
                _innerStream.Dispose();
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
