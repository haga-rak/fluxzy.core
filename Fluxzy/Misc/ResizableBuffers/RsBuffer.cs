using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Fluxzy.Misc.ResizableBuffers
{
    public class RsBuffer : IDisposable
    {
        private RsBuffer(byte[] buffer)
        {
            Buffer = buffer;
        }

        public byte [] Buffer { get; private set;  }

        public static RsBuffer Allocate(int size)
        {
            var rawBuffer = ArrayPool<byte>.Shared.Rent(size);

            var result = new RsBuffer(rawBuffer);

            return result; 
        }

        public void Multiply(int size)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(Buffer.Length * size);
            System.Buffer.BlockCopy(Buffer, 0, newBuffer, 0, Buffer.Length);
            ArrayPool<byte>.Shared.Return(Buffer);
            Buffer = newBuffer;
        }

        public void Extend(int extensionLength)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(Buffer.Length + extensionLength);
            System.Buffer.BlockCopy(Buffer, 0, newBuffer, 0, Buffer.Length);
            ArrayPool<byte>.Shared.Return(Buffer);
            Buffer = newBuffer;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }
}
