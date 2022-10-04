// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Security.Cryptography;

namespace Fluxzy.Tests.Utils
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

        public static string GetStringSha1Hash(Memory<byte> data)
        {
            using var sha = SHA1.Create();

            Span<byte> destination = stackalloc byte[20];

            if (!sha.TryComputeHash(data.Span, destination, out _))
            {
                throw new InvalidOperationException("destination provided to small"); 
            }
            
            return Convert.ToHexString(destination).Replace("-", String.Empty);
        }
        public static string GetStringSha1HashBase64(Memory<byte> data)
        {
            using var sha = SHA1.Create();

            Span<byte> destination = stackalloc byte[20];

            if (!sha.TryComputeHash(data.Span, destination, out _))
            {
                throw new InvalidOperationException("destination provided to small"); 
            }
            
            return Convert.ToBase64String(destination);
        }

        public static string GetStringSha256Hash(byte [] data)
        {
            return GetStringSha1Hash((Memory<byte>) data); 
        }
    }
}