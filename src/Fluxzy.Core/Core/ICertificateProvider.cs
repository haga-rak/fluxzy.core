// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Core
{
    public interface ICertificateProvider : IDisposable
    {
        X509Certificate2 GetCertificate(string hostName);
    }
}
