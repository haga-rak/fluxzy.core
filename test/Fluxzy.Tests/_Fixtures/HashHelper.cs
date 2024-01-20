// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Security.Cryptography;

namespace Fluxzy.Tests._Fixtures
{
    internal static class HashHelper
    {
        public static string MakeWinGetHash(string fileName)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(fileName);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
