using System.Buffers.Binary;
using Fluxzy.Core.Pcap.Pcapng.Structs;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core.Pcap.Pcapng
{
    public static class PcapngUtils
    {
        /// <summary>
        ///     Get the pcapng stream if the nsskey included. Stream  must be seekable
        /// </summary>
        /// <param name="originalStream"></param>
        /// <param name="nssKey"></param>
        /// <returns></returns>
        public static Stream GetPcapngFileWithKeyStream(Stream originalStream, string nssKey)
        {
            // Retrieve the section header block 

            if (!originalStream.CanSeek)
                throw new ArgumentException(nameof(originalStream), "must be seekable");

            if (originalStream.Length < 28)
                throw new ArgumentException(nameof(originalStream), "invalid pcapng file");

            originalStream.Seek(4, SeekOrigin.Begin);

            Span<byte> buffer = stackalloc byte[24];

            originalStream.ReadExact(buffer.Slice(0, 4));

            var blockTotalLength = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(0, 4));

            if (blockTotalLength < 28)
                throw new ArgumentException(nameof(originalStream), "invalid pcapng file");

            if (blockTotalLength > 2048)
                throw new InvalidOperationException("Section header block is too big");

            var block = new NssDecryptionSecretsBlock(nssKey);

            Span<byte> nssKeyBlockBuffer = stackalloc byte[block.BlockTotalLength];

            block.Write(nssKeyBlockBuffer, nssKey);

            var finalBuffer = new byte[blockTotalLength + nssKeyBlockBuffer.Length];

            originalStream.Seek(0, SeekOrigin.Begin);

            originalStream.ReadExact(finalBuffer.AsSpan(0, (int) blockTotalLength));

            nssKeyBlockBuffer.CopyTo(finalBuffer.AsSpan().Slice((int) blockTotalLength));

            return new CombinedReadonlyStream(true, new MemoryStream(finalBuffer), originalStream);
        }

        /// <summary>
        /// Create a PCAPNG file from an existing file with included NSS key
        /// </summary>
        /// <param name="nssKey"></param>
        /// <param name="inRawCaptureStream"></param>
        /// <param name="outFileStream"></param>
        /// <returns></returns>
        public static async Task CreatePcapngFileWithKeysAsync(string nssKey, Stream inRawCaptureStream, Stream outFileStream)
        {
            var pcapStream = inRawCaptureStream;
            await using var tempStream = PcapngUtils.GetPcapngFileWithKeyStream(pcapStream, nssKey);
            await tempStream.CopyToAsync(outFileStream).ConfigureAwait(false);
        }
    }
}
