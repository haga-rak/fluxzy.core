// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Buffers;

namespace Fluxzy.Misc.ResizableBuffers
{
    public class RsBuffer : IDisposable
    {
        public byte[] Buffer { get; private set; }

        public Memory<byte> Memory => new(Buffer);

        private RsBuffer(byte[] buffer)
        {
            Buffer = buffer;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(Buffer);
        }

        public static RsBuffer Allocate(int size)
        {
            var rawBuffer = ArrayPool<byte>.Shared.Rent(size);

            var result = new RsBuffer(rawBuffer);

            return result;
        }

        public void Multiply(int size)
        {
            Extend(Buffer.Length * size);
        }

        public void Extend(int extensionLength)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(Buffer.Length + extensionLength);
            System.Buffer.BlockCopy(Buffer, 0, newBuffer, 0, Buffer.Length);
            ArrayPool<byte>.Shared.Return(Buffer);
            Buffer = newBuffer;

            Console.WriteLine("extend");
        }
    }
}
