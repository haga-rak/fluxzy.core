// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
    public interface ICertificateCache
    {
        byte[] Load(string baseCertificateSerialNumber, string rootDomain, Func<string, byte[]> certificateBuilder);
    }
}
