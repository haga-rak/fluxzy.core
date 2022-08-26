// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Threading.Tasks;

namespace Fluxzy
{
    public class FxzyDirectoryPackager : IDirectoryPackager
    {
        public bool ShouldApplyTo(string fileName)
        {
            return
                fileName.EndsWith(".fxyz", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fxzy", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fzy", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fluxzy", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fxzy.zip", StringComparison.CurrentCultureIgnoreCase) ; 
        }

        public async Task Unpack(Stream inputStream, string directoryOutput)
        {
            await ZipHelper.Decompress(inputStream, new DirectoryInfo(directoryOutput)); 
        }

        public async Task Pack(string directory, Stream outputStream)
        {
            await ZipHelper.Compress(new DirectoryInfo(directory),
                outputStream, fileInfo =>
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
}