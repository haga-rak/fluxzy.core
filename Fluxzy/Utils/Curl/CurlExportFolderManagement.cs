// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading.Tasks;

namespace Fluxzy.Utils.Curl
{
    /// <summary>
    ///    This class is used to manage the temporary folder used by the curl export feature.
    /// </summary>
    public class CurlExportFolderManagement
    {
        public CurlExportFolderManagement(string? temporaryPath = null)
        {
            if (temporaryPath == null) {
                temporaryPath = Environment.GetEnvironmentVariable("FLUXZY_CURL_TEMP_DATA")
                                ?? "%appdata%/Fluxzy.Desktop/temp/curl";

                temporaryPath = Environment.ExpandEnvironmentVariables(temporaryPath);
            }

            TemporaryPath = temporaryPath;
            Directory.CreateDirectory(TemporaryPath);
        }

        public string TemporaryPath { get; internal set; }

        public string GetTemporaryPathFor(Guid fileId)
        {
            return Path.Combine(TemporaryPath, $"temp-payload-{fileId}.bin");
        }

        public Stream? GetTemporaryFileStream(Guid fileId)
        {
            return File.Open(GetTemporaryPathFor(fileId), FileMode.OpenOrCreate,
                FileAccess.Read, FileShare.Read);
        }

        public async Task<bool> SaveTo(Guid fileId, string destinationPath)
        {
            var tempPath = GetTemporaryPathFor(fileId);

            if (!File.Exists(tempPath))
                return false;

            await using var tempStream = File.Open(tempPath, FileMode.Open,
                FileAccess.Read, FileShare.Read);

            await using var destinationStream = File.Open(destinationPath, FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.None);

            await tempStream.CopyToAsync(destinationStream);

            return true;
        }
    }
}
