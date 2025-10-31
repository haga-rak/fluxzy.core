// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;

namespace Fluxzy.Misc.ResizableBuffers
{
    public class RsBuffer : IDisposable
    {
        private RsBuffer(byte[] buffer)
        {
            Buffer = buffer;
        }

        public byte[] Buffer { get; private set; }

        public Memory<byte> Memory => new(Buffer);

        public static RsBuffer Allocate(int size)
        {
            var rawBuffer = ArrayPool<byte>.Shared.Rent(size);

            var result = new RsBuffer(rawBuffer);

            return result;
        }

        public void Multiply(int size)
        {
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size)); 

            Extend(Buffer.Length * size - Buffer.Length);
        }

        public void Extend(int extensionLength)
        {
            if (extensionLength < 0)
                throw new ArgumentOutOfRangeException(nameof(extensionLength));

            if (extensionLength == 0)
                return;

            var forecastLength = Buffer.Length + extensionLength;

            if (forecastLength > FluxzySharedSetting.MaxProcessingBuffer)
                throw new ArgumentOutOfRangeException(nameof(extensionLength), 
                    $@"{nameof(FluxzySharedSetting.MaxProcessingBuffer)} was reached");

            var newBuffer = ArrayPool<byte>.Shared.Rent(forecastLength);

            System.Buffer.BlockCopy(Buffer, 0, newBuffer, 0, Buffer.Length);
            ArrayPool<byte>.Shared.Return(Buffer);

            Buffer = newBuffer;
        }

        public void Ensure(int desiredLength)
        {
            //  Use extend to ensure that the buffer is at least desiredLength long

            if (desiredLength < 0)
                throw new ArgumentOutOfRangeException(nameof(desiredLength));

            if (desiredLength <= Buffer.Length) {
                return; 
            }

            var extensionLength = desiredLength - Buffer.Length;
            Extend(extensionLength);
        }


        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }
}
