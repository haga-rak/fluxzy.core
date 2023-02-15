// Copyright © 2023 Haga RAKOTOHARIVELO

using System;
using System.IO;
using System.Threading.Tasks;

namespace Fluxzy.Utils.Curl
{
    /// <summary>
    /// TODO : inject this class instead of ugly singleton
    /// </summary>
    public static class CurlExportFolderManagement
    {
        static CurlExportFolderManagement()
        {
            TemporaryPath = Environment.GetEnvironmentVariable("FLUXZY_CURL_TEMP_DATA")
                                   ?? "%appdata%/Fluxzy/Curl/Temp";

            TemporaryPath = Environment.ExpandEnvironmentVariables(TemporaryPath);

            Directory.CreateDirectory(TemporaryPath);
        }

        public static string TemporaryPath { get; internal set; }

        public static string GetTemporaryPathFor(Guid fileId)
        {
            return Path.Combine(TemporaryPath, $"temp-payload-{fileId}.bin"); 
        }

        public static Stream?  GetTemporaryFileStream(Guid fileId)
        {
            return File.Open(GetTemporaryPathFor(fileId), FileMode.OpenOrCreate,
                FileAccess.Read, FileShare.Read);
        }

        public static async Task<bool> SaveTo(Guid fileId, string destinationPath)
        {
            var tempPath = GetTemporaryPathFor(fileId);

            if (!File.Exists(tempPath))
            {
                return false;
            }

            await using var tempStream = File.Open(tempPath, FileMode.Open,
                FileAccess.Read, FileShare.Read);
            
            await using var destinationStream = File.Open(destinationPath, FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.None);
            
            await tempStream.CopyToAsync(destinationStream);

            return true; 
        }
    }
}
