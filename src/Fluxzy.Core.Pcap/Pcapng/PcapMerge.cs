// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng
{
    internal static class PcapMerge
    {
        public static void Merge(
            IEnumerable<FileInfo> pcapFiles,
            IEnumerable<FileInfo> nssKeyLogs, Stream outStream, 
            int maxConcurrentOpenFile = 20)
        {
            Merge(pcapFiles.Select(f => new FileStreamSource(f.FullName)),
                nssKeyLogs.Select(f => new FileStreamSource(f.FullName)),
                outStream, maxConcurrentOpenFile);
        }

        public static void Merge(
            IEnumerable<IStreamSource> pcapFiles,
            IEnumerable<IStreamSource> nssKeyLogs, Stream outStream, 
            int maxConcurrentOpenFile = 20)
        {
            var merger = new BlockMerger<IStreamSource>();

            var blockHandler = new PcapBlockWriter(outStream, nssKeyLogs);
            var streamLimiter = new StreamLimiter(maxConcurrentOpenFile);

            merger.Merge(blockHandler, f => new EnhancedBlockReader(blockHandler, streamLimiter, f.Open),
                pcapFiles.ToArray());
        }
    }

    internal interface IStreamSource
    {
        Stream Open(); 
    }

    internal class FileStreamSource : IStreamSource
    {
        private readonly string _fileName;

        public FileStreamSource(string fileName)
        {
            _fileName = fileName;
        }

        public Stream Open()
        {
            return File.OpenRead(_fileName);
        }
    }
}
