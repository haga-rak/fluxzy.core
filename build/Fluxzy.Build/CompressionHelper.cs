// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Fluxzy.Build
{
    public static class CompressionHelper
    {
        internal static void CreateZip(string inputDirectory, string outputFile)
        {
            // add extension if missing 
            
            if (!outputFile.EndsWith(".zip"))
            {
                outputFile += ".zip";
            }
            
            ZipFile.CreateFromDirectory(inputDirectory, outputFile, CompressionLevel.Optimal,
                includeBaseDirectory:false);
        }
        
        public static void CreateCompressed(string inputDirectory, string outputFile)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CreateZip(inputDirectory, outputFile);
            }
            else
            {
                CreateTarGz(inputDirectory, outputFile);
            }
        }
        
        
        internal static void CreateTarGz(string inputDirectory, string outputFile)
        {
            if (!outputFile.EndsWith(".tar.gz"))
            {
                outputFile += ".tar.gz";
            }
            
            using FileStream fs = new(outputFile, FileMode.CreateNew, FileAccess.Write);
            using GZipStream gz = new(fs, CompressionMode.Compress, leaveOpen: true);

            TarFile.CreateFromDirectory(inputDirectory, gz, includeBaseDirectory: false);
        }
    }
}
