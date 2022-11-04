// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Fluxzy.Misc
{
    public static class GuidUtilities
    {
        public static Guid GetMd5Guid(this string str)
        {
            return GetMd5Guid(str.AsSpan());
        }

        public static Guid GetMd5Guid(this ReadOnlySpan<char> inputChars)
        {
            using var md5 = MD5.Create();

            var byteCount = Encoding.ASCII.GetByteCount(inputChars);

            byte[]? heapBuffer = null;

            var buffer =
                byteCount < 1024 ? stackalloc byte[byteCount] : heapBuffer = ArrayPool<byte>.Shared.Rent(byteCount);

            buffer = buffer.Slice(0, byteCount);

            try
            {
                Encoding.ASCII.GetBytes(inputChars, buffer);

                Span<byte> md5Dest = stackalloc byte[16];

                if (!md5.TryComputeHash(buffer, md5Dest, out _))
                    throw new InvalidOperationException("Something very bad happens");

                return new Guid(md5Dest);
            }
            finally
            {
                if (heapBuffer != null)
                    ArrayPool<byte>.Shared.Return(heapBuffer);
            }
        }
    }
}
