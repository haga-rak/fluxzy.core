using System;
using System.Diagnostics;

namespace Echoes.H2.Cli
{
    public readonly ref struct ContinuationFrame
    {
        public ContinuationFrame(ReadOnlyMemory<byte> bodyBytes, HeaderFlags flags, int streamIdentifier)
        {
            EndHeaders = flags.HasFlag(HeaderFlags.EndHeaders);
            Data = bodyBytes;
            StreamIdentifier = streamIdentifier;
            BodyLength = Data.Length;
        }

        public bool EndHeaders { get; }

        public ReadOnlyMemory<byte> Data { get; }

        public int StreamIdentifier { get; }

        public int Write(Span<byte> buffer, ReadOnlySpan<byte> payload = default)
        {
            var offset = H2Frame.Write(buffer, BodyLength, H2FrameType.Continuation, 
                EndHeaders ? HeaderFlags.EndHeaders : HeaderFlags.None, StreamIdentifier);
            
            payload = payload.IsEmpty ? Data.Span : payload;

            payload.CopyTo(buffer.Slice(offset));

            offset += payload.Length; 

            return offset;
        }

        public int BodyLength { get; }
    }
}