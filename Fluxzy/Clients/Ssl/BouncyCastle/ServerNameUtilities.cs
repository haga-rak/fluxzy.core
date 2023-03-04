// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Text;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal static class ServerNameUtilities
    {
        /// <summary>
        ///     This method was created from scratch from a sample WiresharkCapture
        ///     Todo : confirm that no protocol was violated
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static byte[] CreateFromHost(string host)
        {
            var serverNameData = Encoding.UTF8.GetBytes(host);

            Span<byte> buffer = stackalloc byte[1024];

            var totalWritten = 0;

            //BinaryPrimitives.WriteInt16BigEndian(buffer.Slice(totalWritten), (short)(serverNameData.Length + 5));
            //totalWritten += sizeof(short);

            BinaryPrimitives.WriteInt16BigEndian(buffer.Slice(totalWritten), (short) (serverNameData.Length + 3));
            totalWritten += sizeof(short);

            BinaryPrimitives.WriteInt16BigEndian(buffer.Slice(totalWritten), 0);
            totalWritten += sizeof(byte);

            BinaryPrimitives.WriteInt16BigEndian(buffer.Slice(totalWritten), (short) serverNameData.Length);
            totalWritten += sizeof(short);

            serverNameData.CopyTo(buffer.Slice(totalWritten));
            totalWritten += serverNameData.Length;

            return buffer.Slice(0, totalWritten).ToArray();
        }
    }
}
