// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace Fluxzy
{
    /// <summary>
    ///     Utilities for zipping a directory
    /// </summary>
    internal static class ZipHelper
    {
        public static Task DecompressAsync(
            Stream input,
            DirectoryInfo directoryInfo)
        {
            new FastZip().ExtractZip(input, directoryInfo.FullName, FastZip.Overwrite.Always,
                s => true, ".*", ".*", true, true);

            return Task.CompletedTask;
        }

        public static void Decompress(
            Stream input,
            DirectoryInfo directoryInfo)
        {
            new FastZip().ExtractZip(input, directoryInfo.FullName, FastZip.Overwrite.Always,
                s => true, ".*", ".*", true, true);
        }

        public static async Task Compress(
            DirectoryInfo directoryInfo,
            Stream output,
            Func<FileInfo, bool> policy)
        {
            if (!directoryInfo.Exists)
                throw new ArgumentException($"Directory {directoryInfo.FullName} does not exists");

            await using var zipStream = new ZipOutputStream(output) {
                IsStreamOwner = false
            };

            zipStream.SetLevel(3);

            await InternalCompressDirectory(directoryInfo, zipStream, policy);
        }

        public static async Task CompressWithFileInfos(
            DirectoryInfo directoryInfo,
            Stream output, IEnumerable<FileInfo> fileInfos)
        {
            if (!directoryInfo.Exists)
                throw new InvalidOperationException($"Directory {directoryInfo.FullName} does not exists");

            await using var zipStream = new ZipOutputStream(output);

            zipStream.SetLevel(3);

            await InternaCompressDirectoryWithFileInfos(directoryInfo, zipStream, fileInfos);
        }

        private static async Task InternalCompressDirectory(
            DirectoryInfo directoryInfo, ZipOutputStream zipStream,
            Func<FileInfo, bool> policy)
        {
            var fileInfos = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories);
            var directoryName = directoryInfo.FullName;

            foreach (var fileInfo in fileInfos) {
                if (!fileInfo.Exists)
                    continue;

                if (!policy(fileInfo))
                    continue;

                await using var fsInput = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                if (fsInput.Length == 0)
                    continue;

                var entryName = fileInfo.FullName.Replace(directoryName, string.Empty);

                entryName = ZipEntry.CleanName(entryName);

                var newEntry = new ZipEntry(entryName) {
                    DateTime = fileInfo.LastWriteTime
                };

                if (fileInfo.Name.EndsWith("pcap", StringComparison.OrdinalIgnoreCase) ||
                    fileInfo.Name.EndsWith("pcapng", StringComparison.OrdinalIgnoreCase)) {
                    // We don't want to compress pcap files
                    newEntry.CompressionMethod = CompressionMethod.Stored;
                }

                await zipStream.PutNextEntryAsync(newEntry);
                await fsInput.CopyToAsync(zipStream);
                zipStream.CloseEntry();
            }
        }

        private static async Task InternaCompressDirectoryWithFileInfos(
            DirectoryInfo directoryInfo, ZipOutputStream zipStream,
            IEnumerable<FileInfo> fileInfos)
        {
            var directoryName = directoryInfo.FullName;

            foreach (var fileInfo in fileInfos) {
                try {
                    if (!fileInfo.Exists)
                        continue;

                    await using var fsInput = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    if (fsInput.Length == 0)
                        continue;

                    var entryName = fileInfo.FullName.Replace(directoryName, string.Empty);
                    entryName = ZipEntry.CleanName(entryName);

                    var newEntry = new ZipEntry(entryName) {
                        DateTime = fileInfo.LastWriteTime
                    };

                    if (
                        fileInfo.Name.EndsWith("pcap", StringComparison.OrdinalIgnoreCase) ||
                        fileInfo.Name.EndsWith("pcapng", StringComparison.OrdinalIgnoreCase)) {
                        // We don't want to compress pcap files
                        newEntry.CompressionMethod = CompressionMethod.Stored;
                    }

                    await zipStream.PutNextEntryAsync(newEntry);
                    await fsInput.CopyToAsync(zipStream);
                    zipStream.CloseEntry();
                }
                catch (IOException) {
                    // read input is ignored, file may currently used by engine
                }
            }
        }
    }
}
