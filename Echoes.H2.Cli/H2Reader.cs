using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public interface IH2StreamReader
    {
        ValueTask<H2FrameReadResult> ReadNextFrameAsync(Stream stream, byte [] readBuffer, CancellationToken cancellationToken); 
    }

    public class H2Reader : IH2StreamReader
    {
        public async ValueTask<H2FrameReadResult> ReadNextFrameAsync(Stream stream, byte [] readBuffer, CancellationToken cancellationToken)
        {
            await stream.ReadExactAsync(readBuffer, 0, 9, cancellationToken).ConfigureAwait(false);

            var h2FrameHeader = new H2Frame(new ReadOnlySpan<byte>(readBuffer, 0, 9));
            
            await stream.ReadExactAsync(readBuffer, 0, h2FrameHeader.BodyLength, cancellationToken).ConfigureAwait(false);

            switch (h2FrameHeader.BodyType)
            {
                // Setting Frame 
                case H2FrameType.Settings when h2FrameHeader.Flags == 1:
                    // Ack 
                    return new H2FrameReadResult(h2FrameHeader, new SettingFrame(true));
                case H2FrameType.Settings:
                    return new H2FrameReadResult(h2FrameHeader, new SettingFrame(new ReadOnlySpan<byte>(readBuffer, 0 , h2FrameHeader.BodyLength)));
                // WindowUpdate Frame 
                case H2FrameType.WindowUpdate:
                    return new H2FrameReadResult(h2FrameHeader, new WindowUpdateFrame(new ReadOnlySpan<byte>(readBuffer, 0, h2FrameHeader.BodyLength)));
                // Priority Frame 
                case H2FrameType.Priority:
                    return new H2FrameReadResult(h2FrameHeader, new PriorityFrame(new ReadOnlySpan<byte>(readBuffer, 0, h2FrameHeader.BodyLength)));
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}