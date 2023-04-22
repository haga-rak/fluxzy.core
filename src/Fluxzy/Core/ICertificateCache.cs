// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
    public interface ICertificateCache
    {
        byte[] Load(
            string baseCertificatSerialNumber, string rootDomain, Func<string, byte[]> certificateGeneratoringProcess);
    }
}
