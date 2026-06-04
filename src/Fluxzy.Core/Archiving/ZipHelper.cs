// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Fluxzy
{
    /// <summary>
    ///     Utilities for zipping a directory using the runtime's <see cref="System.IO.Compression" />.
    /// </summary>
    internal static class ZipHelper
    {
        // ZipArchiveEntry timestamps must not predate the MS-DOS epoch (1980-01-01).
        private static readonly DateTime DosEpoch = new(1980, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        public static async Task DecompressAsync(
            Stream input,
            DirectoryInfo directoryInfo)
        {
            await InternalDecompress(input, directoryInfo, true);
        }

        public static void Decompress(
            Stream input,
            DirectoryInfo directoryInfo)
        {
            InternalDecompress(input, directoryInfo, false).GetAwaiter().GetResult();
        }

        public static async Task Compress(
            DirectoryInfo directoryInfo,
            Stream output,
            Func<FileInfo, bool> policy)
        {
            if (!directoryInfo.Exists)
                throw new ArgumentException($"Directory {directoryInfo.FullName} does not exists");

            using var zipArchive = new ZipArchive(output, ZipArchiveMode.Create, true);

            foreach (var fileInfo in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)) {
                if (!fileInfo.Exists || !policy(fileInfo))
                    continue;

                await AddEntry(zipArchive, directoryInfo, fileInfo, false);
            }
        }

        public static async Task CompressWithFileInfos(
            DirectoryInfo directoryInfo,
            Stream output, IEnumerable<FileInfo> fileInfos)
        {
            if (!directoryInfo.Exists)
                throw new InvalidOperationException($"Directory {directoryInfo.FullName} does not exists");

            using var zipArchive = new ZipArchive(output, ZipArchiveMode.Create, false);

            foreach (var fileInfo in fileInfos) {
                await AddEntry(zipArchive, directoryInfo, fileInfo, true);
            }
        }

        private static async Task AddEntry(
            ZipArchive zipArchive, DirectoryInfo directoryInfo, FileInfo fileInfo, bool ignoreIoErrors)
        {
            try {
                if (!fileInfo.Exists)
                    return;

                await using var fsInput = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                if (fsInput.Length == 0)
                    return;

                var entryName = CleanEntryName(fileInfo.FullName, directoryInfo.FullName);

                // pcap/pcapng files are already large binary captures that don't compress well.
                var compressionLevel =
                    fileInfo.Name.EndsWith("pcap", StringComparison.OrdinalIgnoreCase) ||
                    fileInfo.Name.EndsWith("pcapng", StringComparison.OrdinalIgnoreCase)
                        ? CompressionLevel.NoCompression
                        : CompressionLevel.Fastest;

                var newEntry = zipArchive.CreateEntry(entryName, compressionLevel);

                var lastWriteTime = fileInfo.LastWriteTime;
                newEntry.LastWriteTime = lastWriteTime < DosEpoch ? DosEpoch : lastWriteTime;

                await using var entryStream = newEntry.Open();
                await fsInput.CopyToAsync(entryStream);
            }
            catch (IOException) when (ignoreIoErrors) {
                // read input is ignored, file may currently be used by engine
            }
        }

        private static async Task InternalDecompress(Stream input, DirectoryInfo directoryInfo, bool useAsync)
        {
            // ZipArchive (Read) needs a seekable stream to locate the central directory.
            Stream seekableInput;
            MemoryStream? bufferedInput = null;

            if (input.CanSeek) {
                seekableInput = input;
            }
            else {
                bufferedInput = new MemoryStream();

                if (useAsync)
                    await input.CopyToAsync(bufferedInput);
                else
                    input.CopyTo(bufferedInput);

                bufferedInput.Position = 0;
                seekableInput = bufferedInput;
            }

            try {
                using var zipArchive = new ZipArchive(seekableInput, ZipArchiveMode.Read, true);

                var destinationRoot = Path.GetFullPath(directoryInfo.FullName);

                Directory.CreateDirectory(destinationRoot);

                foreach (var entry in zipArchive.Entries) {
                    var fullPath = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));

                    // Zip-slip protection: reject entries that resolve outside the destination directory.
                    if (!fullPath.StartsWith(destinationRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        && !string.Equals(fullPath, destinationRoot, StringComparison.Ordinal))
                        continue;

                    // Directory entries have an empty name.
                    if (string.IsNullOrEmpty(entry.Name)) {
                        Directory.CreateDirectory(fullPath);

                        continue;
                    }

                    var parentDirectory = Path.GetDirectoryName(fullPath);

                    if (parentDirectory != null)
                        Directory.CreateDirectory(parentDirectory);

                    await using (var entryStream = entry.Open())
                    await using (var outputFileStream = File.Create(fullPath)) {
                        if (useAsync)
                            await entryStream.CopyToAsync(outputFileStream);
                        else
                            entryStream.CopyTo(outputFileStream);
                    }

                    try {
                        File.SetLastWriteTime(fullPath, entry.LastWriteTime.LocalDateTime);
                    }
                    catch (IOException) {
                        // restoring the timestamp is best-effort
                    }
                    catch (ArgumentOutOfRangeException) {
                        // entry timestamp out of range, ignored
                    }
                }
            }
            finally {
                if (bufferedInput != null)
                    await bufferedInput.DisposeAsync();
            }
        }

        /// <summary>
        ///     Builds a normalized, forward-slash zip entry name relative to <paramref name="directoryName" />.
        /// </summary>
        private static string CleanEntryName(string fullName, string directoryName)
        {
            var entryName = fullName.Replace(directoryName, string.Empty);

            entryName = entryName.Replace('\\', '/');

            return entryName.TrimStart('/');
        }
    }
}
