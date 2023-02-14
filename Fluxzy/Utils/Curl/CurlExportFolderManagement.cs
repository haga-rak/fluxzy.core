// Copyright © 2023 Haga RAKOTOHARIVELO

using System;
using System.IO;

namespace Fluxzy.Utils.Curl
{
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

        public static Stream GetTemporaryFileStream(Guid fileId)
        {
            return File.Open(GetTemporaryPathFor(fileId), FileMode.OpenOrCreate,
                FileAccess.Read, FileShare.Read);
        }
    }
}
