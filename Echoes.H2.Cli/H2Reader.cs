using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public interface IH2FrameReader
    {
        ValueTask<H2FrameReadResult> ReadNextFrameAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken);
    }

    public class H2Reader : IH2FrameReader
    {
        public async ValueTask<H2FrameReadResult> ReadNextFrameAsync(
            Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var headerBuffer = buffer.Slice(0, 9);

            await stream.ReadExactAsync(headerBuffer, cancellationToken).ConfigureAwait(false);

            var frame = new H2Frame(headerBuffer.Span);

            var bodyBuffer = buffer.Slice(0, frame.BodyLength);

            await stream.ReadExactAsync(bodyBuffer, cancellationToken).ConfigureAwait(false);

            return new H2FrameReadResult(frame, bodyBuffer); 
        }
        
    }
}