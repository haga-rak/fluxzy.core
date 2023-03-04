// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Clients.H2
{
    public class H2FrameReader
    {
        public static async ValueTask<H2FrameReadResult> ReadNextFrameAsync(
            Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var headerBuffer = buffer.Slice(0, 9);

            if (!await stream.ReadExactAsync(headerBuffer, cancellationToken).ConfigureAwait(false))
                return default;

            var frame = new H2Frame(headerBuffer.Span);

            if (buffer.Length < frame.BodyLength)
                throw new IOException($"Received frame is too large than MaxFrameSizeAllowed ({frame.BodyLength})");

            var bodyBuffer = buffer.Slice(0, frame.BodyLength);

            if (!await stream.ReadExactAsync(bodyBuffer, cancellationToken).ConfigureAwait(false))
                throw new EndOfStreamException("Unexpected EOF");

            return new H2FrameReadResult(frame, bodyBuffer);
        }

        public static H2FrameReadResult ReadFrame(ref ReadOnlyMemory<byte> inputBuffer)
        {
            var headerBuffer = inputBuffer.Slice(0, 9);
            var frame = new H2Frame(headerBuffer.Span);

            var bodyBuffer = inputBuffer.Slice(0, frame.BodyLength);

            return new H2FrameReadResult(frame, bodyBuffer);
        }
    }
}
