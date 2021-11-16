using System;
using System.Buffers.Binary;
using System.IO;

namespace Echoes.H2.Cli
{
    public readonly struct HeaderFrame : IBodyFrame, IHeaderHolderFrame
    {
        public HeaderFrame(Memory<byte> bodyBytes, bool padded, bool priority, bool endHeader, bool endStream)
        {
            var paddedLength = 0;

            if (padded)
            {
                paddedLength = bodyBytes.Span[0];
                bodyBytes = bodyBytes.Slice(1);
            }

            Priority = priority;
            EndHeader = endHeader;
            EndStream = endStream;

            if (priority)
            {
                Exclusive = (bodyBytes.Span[0] >> 7) == 1;
                StreamDependency = BinaryPrimitives.ReadUInt32BigEndian(bodyBytes.Span) & 0x7FFFFFFF;
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

        public bool Priority { get;  }

        public bool Exclusive { get;  }

        public bool EndHeader { get; }

        public bool EndStream { get;  }

        public uint StreamDependency { get;  }

        public ushort Weight { get;  }

        public Memory<byte> Data { get; }

        public void Write(Stream stream)
        {
        }

        public int BodyLength { get; }
    }
}