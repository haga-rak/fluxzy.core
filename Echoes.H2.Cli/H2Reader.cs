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

            var h2FrameHeader = new H2Frame(headerBuffer.Span);

            var bodyBuffer = buffer.Slice(0, h2FrameHeader.BodyLength);

            await stream.ReadExactAsync(bodyBuffer, cancellationToken).ConfigureAwait(false);

            switch (h2FrameHeader.BodyType)
            {
                // Setting Frame 
                case H2FrameType.Settings when h2FrameHeader.Flags == 1:
                    // Ack 
                    return new H2FrameReadResult(h2FrameHeader, new SettingFrame(true));
                case H2FrameType.Settings:
                    return new H2FrameReadResult(h2FrameHeader, new SettingFrame(bodyBuffer.Span));
                // WindowUpdate Frame 
                case H2FrameType.WindowUpdate:
                    return new H2FrameReadResult(h2FrameHeader, new WindowUpdateFrame(bodyBuffer.Span));
                // Priority Frame 
                case H2FrameType.Priority:
                    return new H2FrameReadResult(h2FrameHeader, new PriorityFrame(bodyBuffer.Span));
                case H2FrameType.Data:
                    return new H2FrameReadResult(h2FrameHeader, new DataFrame(
                        bodyBuffer, (h2FrameHeader.Flags & 0x8) != 0, (h2FrameHeader.Flags & 0x1) != 0));
                case H2FrameType.Headers:
                    return new H2FrameReadResult(h2FrameHeader, new HeaderFrame(bodyBuffer, 
                        (h2FrameHeader.Flags & 0x8) != 0, 
                        (h2FrameHeader.Flags & 0x20) != 0, 
                        (h2FrameHeader.Flags & 0x4) != 0, 
                        (h2FrameHeader.Flags & 0x1) != 0)
                        );
                case H2FrameType.Continuation:
                    return new H2FrameReadResult(h2FrameHeader, new ContinuationFrame(bodyBuffer, 
                        (h2FrameHeader.Flags & 0x4) != 0)
                        );
                default:
                    throw new InvalidOperationException();
            }
        }
        
    }

    public class H2Packetizer
    {

    }
}