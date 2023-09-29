// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fluxzy
{
    [PackagerInformation("fluxzy", "The fluxzy archive format", ".fxzy", ".fzy", ".fluxzy")]
    public class FxzyDirectoryPackager : DirectoryPackager
    {
        public override bool ShouldApplyTo(string fileName)
        {
            return
                fileName.EndsWith(".fxzy", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fluxzy", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fxzy.zip", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".fluxzy.zip", StringComparison.CurrentCultureIgnoreCase);
        }

        public async Task Pack(string directory, string outputFileName, HashSet<int>? exchangeIds = null)
        {
            using var outputStream = new FileStream(outputFileName, FileMode.Create);
            await Pack(directory, outputStream, exchangeIds);
        }

        public override async Task Pack(string directory, Stream outputStream, HashSet<int>? exchangeIds)
        {
            var baseDirectory = new DirectoryInfo(directory);

            var packableFiles =
                GetPackableFileInfos(baseDirectory, exchangeIds);

            await ZipHelper.CompressWithFileInfos(baseDirectory, outputStream, packableFiles.Select(s => s.File));
        }

        public async Task UnpackAsync(Stream inputStream, string directoryOutput)
        {
            await ZipHelper.DecompressAsync(inputStream, new DirectoryInfo(directoryOutput));
        }

        public void Unpack(Stream inputStream, string directoryOutput)
        {
             ZipHelper.Decompress(inputStream, new DirectoryInfo(directoryOutput));
        }
    }
}
