// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Security.Cryptography;

namespace Fluxzy.Build
{
    internal static class HashHelper
    {
        public static string GetSha512Hash(FileInfo file)
        {
            using var sha512 = SHA512.Create();
            using var stream = file.OpenRead();
            var hash = sha512.ComputeHash(stream);

            return Convert.ToHexString(hash).ToLower();
        }

        public static string GetWinGetHash(string filePath)
        {
            return GetWinGetHash(new FileInfo(filePath));
        }

        public static string GetWinGetHash(FileInfo fileInfo)
        {
            using var sha256 = SHA256.Create();
            using var stream = fileInfo.OpenRead();
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
