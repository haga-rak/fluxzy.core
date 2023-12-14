// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text;

namespace Fluxzy.Tests._Fixtures
{
    internal static class ToBytesExtension
    {
        public static byte[] ToBytes(this string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }
    }
}
