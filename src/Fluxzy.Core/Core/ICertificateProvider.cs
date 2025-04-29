// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Core
{
    /// <summary>
    /// A certificate provider for Fluxzy
    /// </summary>
    public interface ICertificateProvider : IDisposable
    {
        /// <summary>
        /// Retrieve a certificate for a particular host 
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        X509Certificate2 GetCertificate(string hostName);
    }
}
