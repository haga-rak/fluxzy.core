using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Helpers;

namespace Echoes.H2
{

    public class H2FrameReader
    {
        public static async ValueTask<H2FrameReadResult> ReadNextFrameAsync(
            Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var headerBuffer = buffer.Slice(0, 9);

            await stream.ReadExactAsync(headerBuffer, cancellationToken).ConfigureAwait(false);

            var frame = new H2Frame(headerBuffer.Span);

            var bodyBuffer = buffer.Slice(0, frame.BodyLength);

            await stream.ReadExactAsync(bodyBuffer, cancellationToken).ConfigureAwait(false);

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