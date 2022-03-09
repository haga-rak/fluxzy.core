using System;

namespace Echoes.Clients.H2.Frames
{
    public readonly ref struct DataFrame
    {
        public DataFrame(ReadOnlyMemory<byte> bodyBytes, HeaderFlags flags, int streamIdentifier)
        {
            EndStream = flags.HasFlag(HeaderFlags.EndStream);
            StreamIdentifier = streamIdentifier;
            var paddedLength = 0;

            var padded = flags.HasFlag(HeaderFlags.Padded);

            if (padded)
            {
                paddedLength = bodyBytes.Span[0];
                bodyBytes = bodyBytes.Slice(1);
            }

            BodyLength = bodyBytes.Length - paddedLength;
            Buffer = bodyBytes.Slice(0, BodyLength);
        }

        public DataFrame(HeaderFlags flags, int bodyLength, int streamIdentifier)
        {
            EndStream = flags.HasFlag(HeaderFlags.EndStream);
            Buffer = default;
            BodyLength = bodyLength;
            StreamIdentifier = streamIdentifier;
        }

        public bool EndStream { get; }

        public ReadOnlyMemory<byte> Buffer { get; }

        public int WriteHeaderOnly(Span<byte> buffer, int bodyLength)
        {
            var offset = H2Frame.Write(buffer, bodyLength, H2FrameType.Data, EndStream ? HeaderFlags.EndStream : HeaderFlags.None , StreamIdentifier);
            
            return offset; 
        }

        public int BodyLength { get; }

        public int StreamIdentifier { get; }
    }
}