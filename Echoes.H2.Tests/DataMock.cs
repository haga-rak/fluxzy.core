// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Security.Cryptography;

namespace Echoes.H2.Tests
{
    public static class MockedBinaryUtilities
    {
        public static byte [] GenerateRng(int seed, int size)
        {
            Random random = new Random(seed);  
            var buffer = new byte[size];
            random.NextBytes(buffer);

            return buffer; 

        }

        public static string GetStringSha256Hash(byte [] data)
        {
            using var sha = SHA1.Create();
            return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", String.Empty);
        }
    }
    
}