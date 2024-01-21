// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO.Compression;

namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    /// <summary>
    ///  PcapMerge is an utility helper for combining multiple pcap streams to a single one.
    ///  This implementation is intended only for merging pcap files
    ///  produced by fluxzy and does not cover pcap files that comes from other sources.
    ///  If it's the goal you want, you should consider using mergecap (https://gitlab.com/wireshark/wireshark/-/blob/master/mergecap.c)
    /// </summary>
    public static class PcapMerge
    {
        /// <summary>
        ///     Merge from provided files
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
        ///     Merge from StreamSoruce
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
        ///     Merge from a fluxzy dump directory
        /// </summary>
        /// <param name="dumpDirectory"></param>
        /// <param name="outStream"></param>
        /// <param name="maxConcurrentOpenFile"></param>
        /// <param name="connectionIds"></param>
        public static void MergeDumpDirectory(
            string dumpDirectory, Stream outStream,
            int maxConcurrentOpenFile = 20,
            HashSet<int>? connectionIds = null)
        {
            var captureDirectory = Path.Combine(dumpDirectory, "captures");

            if (!Directory.Exists(captureDirectory)) {
                return;
            }

            var pcapFiles = new DirectoryInfo(captureDirectory)
                .EnumerateFiles("*.pcapng");

            if (connectionIds != null) {
                pcapFiles =
                    pcapFiles.Where(p => FilterConnectionHelper.CheckInList(p.Name,
                        connectionIds));
            }

            var nssKeys = new DirectoryInfo(captureDirectory)
                .EnumerateFiles("*.nsskeylog");

            if (connectionIds != null) {
                nssKeys =
                    nssKeys.Where(p => FilterConnectionHelper.CheckInList(p.Name,
                        connectionIds));
            }

            Merge(pcapFiles, nssKeys, outStream, maxConcurrentOpenFile);
        }

        /// <summary>
        ///     Merge from a fluxzy dump directory
        /// </summary>
        /// <param name="archiveFile"></param>
        /// <param name="outStream"></param>
        /// <param name="connectionIds"></param>
        public static void MergeArchive(
            string archiveFile, Stream outStream,
            HashSet<int>? connectionIds = null)
        {
            using var zipArchive = ZipFile.OpenRead(archiveFile);

            var allEntries =
                zipArchive.Entries.Where(e => e.FullName.StartsWith("captures"))
                          .ToList();

            var capEntries = allEntries
                .Where(e => e.Name.EndsWith(".pcapng"));

            if (connectionIds != null) {
                capEntries =
                    capEntries.Where(p => FilterConnectionHelper.CheckInList(p.Name,
                        connectionIds));
            }

            var nssKeys = allEntries
                .Where(e => e.Name.EndsWith(".nsskeylog"));

            if (connectionIds != null) {
                nssKeys =
                    nssKeys.Where(p => FilterConnectionHelper.CheckInList(p.Name,
                        connectionIds));
            }

            Merge(
                capEntries.Select(c => new ZipStreamSource(c)),
                nssKeys.Select(c => new ZipStreamSource(c)), outStream,
                int.MaxValue);
        }
    }
}
