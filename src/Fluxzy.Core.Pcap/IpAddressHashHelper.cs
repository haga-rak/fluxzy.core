// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fluxzy.Core.Pcap
{
    internal static class IpAddressHashHelper
    {
        public static int Get4BytesHash(this IPAddress address)
        {
            Span<byte> bouff = stackalloc byte[16];
            int offset, length;

            if (address.IsIPv4MappedToIPv6) {
                address.TryWriteBytes(bouff, out _);
                length = 4;
                offset = 12;
            }
            else
            {
                address.TryWriteBytes(bouff, out length);
                offset = 0;
            }

            return GetHashCode(bouff.Slice(offset, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(ReadOnlySpan<byte> span)
        {
            if (span.Length == 4) {
                return MemoryMarshal.Read<int>(span);
            }

            Span<byte> tmp = stackalloc byte[8];

            span.Slice(0, Math.Min(span.Length, 8)).CopyTo(tmp);
            ulong lo = MemoryMarshal.Read<ulong>(tmp);

            ulong hi = 0;
            if (span.Length > 8)
            {
                int tailLen = span.Length - 8;
                Span<byte> tmp2 = stackalloc byte[8];
                span.Slice(8, tailLen).CopyTo(tmp2);
                hi = MemoryMarshal.Read<ulong>(tmp2);
            }

            const ulong k = 0x9E3779B97F4A7C15UL;
            ulong mixed = lo ^ (hi * k);

            return (int)((mixed >> 32) ^ mixed);
        }
    }
}
