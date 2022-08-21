using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace Fluxzy
{
    /// <summary>
    /// Utilities for zipping a directory
    /// </summary>
    public static class ZipHelper
    {
        public static Task Decompress(
            Stream input,
            DirectoryInfo directoryInfo)
        {
            if (directoryInfo.Exists)
                Directory.CreateDirectory(directoryInfo.FullName); 
            
            new FastZip().ExtractZip(input, directoryInfo.FullName, FastZip.Overwrite.Always,
                s => true, "*", "*", true, true);

            return Task.CompletedTask;
        }


        public static async Task Compress(DirectoryInfo directoryInfo, 
            Stream output,
            Func<FileInfo, bool> policy)
        {
            if (!directoryInfo.Exists)
                throw new InvalidOperationException($"Directory {directoryInfo.FullName} does not exists");

            await using var zipStream = new ZipOutputStream(output);

            zipStream.SetLevel(3);

            await InternaCompressDirectory(directoryInfo, zipStream, 0, policy);
        }
        
        private static async Task InternaCompressDirectory(
            DirectoryInfo directoryInfo, ZipOutputStream zipStream, int folderOffset,
            Func<FileInfo, bool> policy)
        {
            var fileInfos = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories);
            var directoryName = directoryInfo.FullName; 

            foreach (var fi in fileInfos)
            {
                if (!policy(fi))
                    continue;

                var entryName = fi.FullName.Replace(directoryName, string.Empty);

                entryName = ZipEntry.CleanName(entryName);

                var newEntry = new ZipEntry(entryName)
                {
                    DateTime = fi.LastWriteTime
                };

                zipStream.PutNextEntry(newEntry);

                await using (var fsInput = fi.OpenRead())
                {
                    await fsInput.CopyToAsync(zipStream);
                }

                zipStream.CloseEntry();
            }
        }
    }
}