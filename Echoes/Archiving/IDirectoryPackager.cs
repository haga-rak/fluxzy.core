using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace Echoes
{
    public interface IDirectoryPackager
    {
        bool ShouldApplyTo(string fileName); 

        Task Pack(string directory, Stream output);
    }

    public class EczDirectoryPackager : IDirectoryPackager
    {
        public bool ShouldApplyTo(string fileName)
        {
            return
                fileName.EndsWith(".ecz", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".eczip", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".ec.zip", StringComparison.CurrentCultureIgnoreCase) ; 
        }

        public async Task Pack(string directory, Stream output)
        {
            await ZipHelper.Compress(new DirectoryInfo(directory),
                output, fileInfo =>
                {
                    if (fileInfo.Length == 0)
                        return false;

                    if (!fileInfo.Name.EndsWith(".data")
                        && !fileInfo.Name.EndsWith(".json"))
                    {
                        return false; 
                    }

                    return true; 
                }); 
        }
    }

    public static class ZipHelper
    {
        public static async Task Compress(DirectoryInfo directoryInfo, 
            Stream output,
            Func<FileInfo, bool> policy)
        {
            if (!directoryInfo.Exists)
                throw new InvalidOperationException($"Directory {directoryInfo.FullName} does not exists");

            using var zipStream = new ZipOutputStream(output);

            zipStream.SetLevel(3);

            await CompressFolder(directoryInfo, zipStream, 0, policy);
        }


        // Recursively compresses a folder structure
        private static async Task CompressFolder(
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

            //var folders = directoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories);

            //foreach (var folder in folders)
            //{
            //    await CompressFolder(folder, zipStream, folderOffset, policy);
            //}
        }
    }
}