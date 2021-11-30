using System;

namespace Echoes.H2.Cli
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

        public int Write(Span<byte> buffer, ReadOnlySpan<byte> payload = default)
        {
            var toWrite = payload.Length == 0 ? Buffer.Span : payload;
            var offset = H2Frame.Write(buffer, toWrite.Length, H2FrameType.Data, EndStream ? HeaderFlags.EndStream : HeaderFlags.None , StreamIdentifier);
           

            toWrite.CopyTo(buffer.Slice(offset));
            return offset + toWrite.Length; 
        }

        public int BodyLength { get; }

        public int StreamIdentifier { get; }
    }
}