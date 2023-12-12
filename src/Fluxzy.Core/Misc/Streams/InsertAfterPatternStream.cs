// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;

namespace Fluxzy.Misc.Streams;

public class InsertAfterPatternStream : Stream
{
    // The original stream 
    private readonly Stream _innerStream;

    // The stream to be injected
    private readonly Stream _injectedStream;

    // The matcher 
    private readonly IBinaryMatcher _binaryMatcher;

    // The pattern to match
    private readonly byte[] _matchingPattern;


    private readonly byte [] _internalBuffer;

    // Validated buffer that waits to be read 
    private readonly byte [] _pendingValidatedBuffer;

    // Validated buffer length
    private int _pendingValidatedBufferLength; 

    // Previous read that was not validated yet
    private readonly byte[] _pendingUnvalidatedBuffer;
    
    // Unvalidated buffer length
    private int _pendingUnvalidatedBufferLength;

    // Flag indicating that injection should start
    private bool _continueInjecting;

    public InsertAfterPatternStream(Stream innerStream,
        IBinaryMatcher binaryMatcher,
        byte[] matchingPattern, 
        Stream injectedStream)
    {
        _innerStream = innerStream;
        _binaryMatcher = binaryMatcher;
        _matchingPattern = matchingPattern;
        _injectedStream = injectedStream;

        var bufferSize = Math.Max(_matchingPattern.Length * 2, 4096);

        _internalBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        _pendingValidatedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
        _pendingUnvalidatedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
    }

    public override void Flush()
    {
    }

    private bool _innerStreamEof;

    public override int Read(byte[] buffer, int offset, int count)
    {
        while (true) {
            // Copy pendingBuffer to reader 

            if (_pendingValidatedBufferLength > 0)
            {
                var readable = Math.Min(count, _pendingValidatedBufferLength);
                _pendingValidatedBuffer.AsSpan(0, readable).CopyTo(buffer.AsSpan(offset));

                // Shift buffer 

                BufferArrayShiftUtilities.ShiftOffsetToZero(_pendingValidatedBuffer, readable, _pendingValidatedBufferLength - readable);

                _pendingValidatedBufferLength -= readable;
                return readable;
            }

            // Drain zone copy to the reader

            if (_continueInjecting) {
                var injectRead = _injectedStream.Read(buffer, offset, count);
                _continueInjecting = injectRead > 0;

                if (injectRead > 0)
                    return injectRead; // Inject the datas
            }

            if (_innerStreamEof)
                break; 

            var canBeStored = 
                _pendingUnvalidatedBuffer.Length - _pendingUnvalidatedBufferLength;

            var maxRead = Math.Min(buffer.Length, canBeStored); // MAYBE not usefull ? 
            
            var read = _innerStream.Read(_pendingUnvalidatedBuffer,
                _pendingUnvalidatedBufferLength, maxRead);

            _pendingUnvalidatedBufferLength += read;


            if (read == 0) {

                _innerStreamEof = true; 

                if (_pendingUnvalidatedBufferLength == 0)
                {
                    // EOF 
                    return 0;
                }
                else {
                    Array.Copy(_pendingUnvalidatedBuffer, 0, 
                        _pendingValidatedBuffer, 0, _pendingUnvalidatedBufferLength);

                    continue; 
                }
            }

            var index = _binaryMatcher
                .FindIndex(_pendingUnvalidatedBuffer.AsSpan(0, _pendingUnvalidatedBufferLength), 
                    _matchingPattern);

            if (index < 0) {

                var validatedLength = _pendingUnvalidatedBufferLength - _matchingPattern.Length;

                if (validatedLength > 0) {
                    // validate _pendingUnvalidatedBufferLength - _matchingPattern.Length

                    _pendingUnvalidatedBuffer.AsSpan(0, validatedLength).CopyTo(_pendingValidatedBuffer);

                    BufferArrayShiftUtilities.ShiftOffsetToZero(_pendingUnvalidatedBuffer,
                        validatedLength, _matchingPattern.Length);

                    _pendingUnvalidatedBufferLength -= validatedLength;
                    _pendingValidatedBufferLength = validatedLength;

                    continue; 
                }
            }
            else {
                // Copy to validated buffer 

                var validatedLength = index + _matchingPattern.Length;

                _pendingUnvalidatedBuffer.AsSpan(0, validatedLength).CopyTo(_pendingValidatedBuffer);

                BufferArrayShiftUtilities.ShiftOffsetToZero(_pendingUnvalidatedBuffer,
                                           validatedLength, _pendingUnvalidatedBufferLength - validatedLength);

                _pendingUnvalidatedBufferLength -= validatedLength;
                _pendingValidatedBufferLength = validatedLength;

                // Copy to reader 

                _continueInjecting = true; 

                continue; 
            }
            
        }

        
        return -1; 
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

    public override bool CanRead { get; } = true; 

    public override bool CanSeek { get; } = false;

    public override bool CanWrite { get; } = false; 

    public override long Length  => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}

internal static class BufferArrayShiftUtilities
{
    public static void ShiftOffsetToZero(byte[] buffer, int offset, int length)
    {
        Array.Copy(buffer, offset, buffer, 0, length);
    }
}