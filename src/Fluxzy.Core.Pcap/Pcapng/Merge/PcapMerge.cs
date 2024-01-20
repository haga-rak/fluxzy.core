// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    public static class PcapMerge
    {
        /// <summary>
        ///  Merge from provided files
        /// </summary>
        /// <param name="pcapFiles"></param>
        /// <param name="nssKeyLogs"></param>
        /// <param name="outStream"></param>
        /// <param name="maxConcurrentOpenFile"></param>
        public static void Merge(
            IEnumerable<FileInfo> pcapFiles,
            IEnumerable<FileInfo> nssKeyLogs, Stream outStream,
            int maxConcurrentOpenFile = 20)
        {
            Merge(pcapFiles.Select(f => new FileStreamSource(f.FullName)),
                nssKeyLogs.Select(f => new FileStreamSource(f.FullName)),
                outStream, maxConcurrentOpenFile);
        }

        /// <summary>
        ///  Merge from StreamSoruce
        /// </summary>
        /// <param name="pcapFiles"></param>
        /// <param name="nssKeyLogs"></param>
        /// <param name="outStream"></param>
        /// <param name="maxConcurrentOpenFile"></param>
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

        /// <summary>
        ///  Merge from a fluxzy dump directory 
        /// </summary>
        /// <param name="dumpDirectory"></param>
        /// <param name="outStream"></param>
        /// <param name="maxConcurrentOpenFile"></param>
        public static void Merge(string dumpDirectory, Stream outStream, int maxConcurrentOpenFile = 20)
        {
            var captureDirectory = Path.Combine(dumpDirectory, "captures");

            if (!Directory.Exists(captureDirectory))
                return; 

            var pcapFiles = new DirectoryInfo(captureDirectory)
                .EnumerateFiles("*.pcapng"); 

            var nssKeys = new DirectoryInfo(captureDirectory)
                .EnumerateFiles("*.nsskeylog");

            Merge(pcapFiles, nssKeys, outStream, maxConcurrentOpenFile);
        }
    }

    public interface IStreamSource
    {
        Stream Open();
    }

    /// <summary>
    ///  A classic stream from an existing file 
    /// </summary>
    public class FileStreamSource : IStreamSource
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
