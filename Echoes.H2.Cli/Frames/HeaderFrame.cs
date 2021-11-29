using System;
using System.Buffers.Binary;
using System.IO;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public readonly struct HeaderFrame : IPriorityFrame, IBodyFrame, IHeaderHolderFrame
    {
        public HeaderFrame(ReadOnlyMemory<byte> bodyBytes, bool padded, bool priority, bool endHeader, bool endStream)
        {
            byte paddedLength = 0;

            Padded = padded;

            if (padded)
            {
                paddedLength = bodyBytes.Span[0];
                bodyBytes = bodyBytes.Slice(1);
            }

            PadLength = paddedLength;

            Priority = priority;
            EndHeader = endHeader;
            EndStream = endStream;

            if (priority)
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

        public HeaderFrame(
            bool padded, 
            byte paddedLength, bool priority,
            bool endHeader, bool endStream, byte weight,
            bool exclusive, int streamDependency)
        {
            
            Padded = padded;
            Priority = priority;
            EndHeader = endHeader;
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

        public bool EndHeader { get; }

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