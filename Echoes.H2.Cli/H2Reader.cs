using System;
using System.IO;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{

    public static class H2Writer
    {
        public static async Task WriteFrameAsync(IBodyFrame bodyFrame)
        {
            
        }
    }

    public static class H2Reader
    {
        public static async Task<H2FrameReadResult> ReadNextFrameAsync(Stream stream)
        {
            byte[] header = new byte[9];

            await stream.ReadExact(header, 0, header.Length).ConfigureAwait(false);

            var h2FrameHeader = new H2Frame(header);

            var bodyBytes = new byte[h2FrameHeader.Length];

            await stream.ReadExact(bodyBytes, 0, bodyBytes.Length);

            if (h2FrameHeader.BodyType == H2FrameType.Settings) // Setting Frame 
            {
                return new H2FrameReadResult(h2FrameHeader, new SettingFrame(new ReadOnlySpan<byte>(bodyBytes)));
            }

            if (h2FrameHeader.BodyType == H2FrameType.WindowUpdate) // WindowUpdate Frame 
            {
                return new H2FrameReadResult(h2FrameHeader, new WindowUpdateFrame(bodyBytes));
            }

            if (h2FrameHeader.BodyType == H2FrameType.Priority) // Priority Frame 
            {
                return new H2FrameReadResult(h2FrameHeader, new PriorityFrame(bodyBytes));
            }

            throw new InvalidOperationException();
        }
    }
}