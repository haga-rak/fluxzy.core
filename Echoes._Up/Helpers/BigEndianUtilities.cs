using System;
using System.Buffers.Binary;
using System.IO;

namespace Echoes.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public static class BigEndianUtilities
    {
        public static Stream BuWrite_8(this Stream stream, byte data)
        {
            Span<byte> buffer = stackalloc byte[1];

            buffer[0] = data;
            stream.Write(buffer);

            return stream; 
        }
        public static Span<byte> BuWrite_8(this Span<byte> buffer, byte data)
        {
            buffer[0] = data;

            return buffer.Slice(1); 
        }
        
        public static Stream BuWrite_32(this Stream stream, uint data)
        {
            Span<byte> buffer = stackalloc byte[4];

            BinaryPrimitives.WriteUInt32BigEndian(buffer, data);

            stream.Write(buffer);

            return stream; 
        }

        public static Span<byte> BuWrite_32(this Span<byte> buffer, uint data)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, data);
            return buffer.Slice(4); 
        }

        public static Span<byte> BuWrite_32(this Span<byte> buffer, int data)
        {
            return buffer.BuWrite_32((uint) data); 
        }

        public static Stream BuWrite_32(this Stream stream, int data)
        {
            return stream.BuWrite_32((uint) data); 
        }

        public static Stream BuWrite_24(this Stream stream, uint data)
        {
            Span<byte> buffer = stackalloc byte[3];
            
            buffer[0] = (byte)((0x00ff0000 & data) >> 16); 
            buffer[1] = (byte)((0x0000ff00 & data) >> 8); 
            buffer[2] = (byte)((0x000000ff & data));

            stream.Write(buffer);

            return stream; 
        }
        public static Span<byte> BuWrite_24(this Span<byte> buffer, uint data)
        {
            buffer[0] = (byte)((0x00ff0000 & data) >> 16); 
            buffer[1] = (byte)((0x0000ff00 & data) >> 8); 
            buffer[2] = (byte)((0x000000ff & data));

            return buffer.Slice(3);
        }

        public static Span<byte> BuWrite_24(this Span<byte> buffer, int data)
        {
            return buffer.BuWrite_24((uint)data);
        }

        public static Stream BuWrite_24(this Stream stream, int data)
        {
            return stream.BuWrite_24((uint) data); 
        }


        public static Stream BuWrite_16(this Stream stream, short data)
        {
            return stream.BuWrite_16((ushort) data); 
        }

        public static Span<byte> BuWrite_16(this Span<byte> buffer, short data)
        {
            return buffer.BuWrite_16((ushort) data); 
        }

        public static Stream BuWrite_16(this Stream stream, ushort data)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, data);
            stream.Write(buffer);
            return stream; 
        }

        public static Span<byte> BuWrite_16(this Span<byte> buffer, ushort data)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer, data);
            return buffer.Slice(2); 
        }

        public static Stream BuWrite_1_31(this Stream stream, bool _1, uint _31)
        {
            uint finalData = !_1 ? (_31 & 0x7FFFFFFF) : (uint) (_31 | 0x80000000);
            return BuWrite_32(stream, finalData); 
        }


        public static Span<byte> BuWrite_1_31(this Span<byte> buffer, bool _1, uint _31)
        {
            uint finalData = !_1 ? (_31 & 0x7FFFFFFF) : (uint) (_31 | 0x80000000);
            return BuWrite_32(buffer, finalData); 
        }

        public static Span<byte> BuWrite_1_31(this Span<byte> buffer, bool _1, int _31)
        {
            return BuWrite_1_31(buffer, _1, (uint)_31);
        }
    }
}