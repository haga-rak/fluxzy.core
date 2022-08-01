using System;
using System.Buffers.Binary;
using Echoes.Misc;

namespace Echoes.Clients.H2.Frames
{
    public readonly ref struct HeadersFrame
    {
        public HeadersFrame(ReadOnlyMemory<byte> bodyBytes, HeaderFlags flags)
        {
            byte paddedLength = 0;

            Padded = flags.HasFlag(HeaderFlags.Padded);

            if (Padded)
            {
                paddedLength = bodyBytes.Span[0];
                bodyBytes = bodyBytes.Slice(1);
            }

            PadLength = paddedLength;

            Priority = flags.HasFlag(HeaderFlags.Priority); 
            EndHeaders = flags.HasFlag(HeaderFlags.EndHeaders);
            EndStream = flags.HasFlag(HeaderFlags.EndStream); ;

            if (Priority)
            {
                Exclusive = (bodyBytes.Span[0] >> 7) == 1;
                StreamDependency = BinaryPrimitives.ReadInt32BigEndian(bodyBytes.Span) & 0x7FFFFFFF;
                Weight = bodyBytes.Span[4];
                bodyBytes.Slice(5);
            }
            else
            {
                Exclusive = false;
                StreamDependency = 0;
                Weight = 0; 
            }
            
            BodyLength = bodyBytes.Length - paddedLength;
            Data = bodyBytes.Slice(0, BodyLength);
        }

        public HeadersFrame(
            bool padded, 
            byte paddedLength, bool priority,
            bool endHeaders, bool endStream, byte weight,
            bool exclusive, int streamDependency)
        {
            
            Padded = padded;
            Priority = priority;
            EndHeaders = endHeaders;
            EndStream = endStream;
            Weight = weight;
            Data = default;
            PadLength = paddedLength;
            Exclusive = exclusive;
            StreamDependency = streamDependency;

            var initialLength = 6;

            if (!Priority)
            {
                initialLength -= 5;
            }
            if (Padded)
            {
                initialLength -= 1;
            }

            BodyLength = initialLength;
        }

        public byte PadLength { get; } 

        public bool Padded { get; }

        public bool Priority { get;  }

        public bool Exclusive { get;  }

        public bool EndHeaders { get; }

        public bool EndStream { get;  }

        public int StreamDependency { get;  }

        public byte Weight { get;  }

        public ReadOnlyMemory<byte> Data { get; }

        public int Write(Span<byte> buffer, ReadOnlySpan<byte> payload = default)
        {
            var written = 0; 

            if (Padded)
            {
                buffer = buffer.BuWrite_8(PadLength);
                written += 1;
            }

            var dependency = StreamDependency;

            if (Padded && Exclusive)
            {
                dependency |= (0x7FFF_FFFF);
            }

            if (Priority)
            {
                buffer = buffer.BuWrite_32(dependency);
                buffer = buffer.BuWrite_8(Weight);

                written += 5;
            }

            payload.CopyTo(buffer);
            buffer = buffer.Slice(payload.Length);
            written += payload.Length;

            if (Padded && PadLength > 0)
            {
                buffer = buffer.Slice(PadLength);
                written += PadLength;
            }

            return written; 
        }

        public int BodyLength { get; }
    }
}