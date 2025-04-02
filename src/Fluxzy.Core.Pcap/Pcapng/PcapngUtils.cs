using System.Buffers.Binary;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Core.Pcap.Pcapng.Structs;
using Fluxzy.Misc.Streams;
using Fluxzy.Writers;

namespace Fluxzy.Core.Pcap.Pcapng
{
    public static class PcapngUtils
    {
        /// <summary>
        ///    Create a HttpMessageHandler that will write the pcapng file to the specified location
        /// </summary>
        /// <param name="outPcapFileName"></param>
        /// <param name="sslProvider"></param>
        /// <returns></returns>
        public static async Task<HttpMessageHandler> CreateHttpHandler(string outPcapFileName,
            SslProvider sslProvider = SslProvider.BouncyCastle)
        {
            var proxyScope = new ProxyScope(() => null!, a => new OutOfProcessCaptureContext(a));

            var tcpProvider = await CapturedTcpConnectionProvider.Create(proxyScope, false);

            var disposables = new List<IAsyncDisposable> { tcpProvider, proxyScope };

            var fluxzyDefaultHandler = new FluxzyDefaultHandler(
                sslProvider, tcpProvider,
                new PcapOnlyArchiveWriter(_ => outPcapFileName),
                disposables: disposables);

            return fluxzyDefaultHandler;
        }

        /// <summary>
        ///    Read the pcapng file with the included keys if available.
        ///    SslKeyLogFile must be in the same directory as the pcapng file,
        ///    with the same name but with the extension .nsskeylog
        /// </summary>
        /// <param name="pcapngFile"></param>
        /// <returns></returns>
        public static Stream ReadWithKeysAsync(string pcapngFile)
        {
            var directory = new FileInfo(pcapngFile).DirectoryName;

            var sslKeyLogFile = directory == null ? $"{Path.GetFileNameWithoutExtension(pcapngFile)}.nsskeylog" :
                Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(pcapngFile)}.nsskeylog");

            if (!File.Exists(sslKeyLogFile))
                return File.OpenRead(pcapngFile);

            var nssKey = File.ReadAllText(sslKeyLogFile);
            return GetPcapngFileWithKeyStream(File.OpenRead(pcapngFile), nssKey);
        }

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
