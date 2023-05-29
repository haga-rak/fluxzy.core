// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Security.Cryptography;

namespace Fluxzy.Tests._Fixtures
{
    internal static class FileUtilities
    {
        public static string DrainAndSha1(this Stream stream)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(stream);
            var base64String = Convert.ToBase64String(hash);

            return base64String;
        }

        public static bool CompareStream(Stream a, Stream b)
        {
            return string.Equals(a.DrainAndSha1(), b.DrainAndSha1());
        }
    }
}
