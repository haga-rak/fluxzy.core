// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Certificates
{
    public enum CertificateRetrieveMode
    {
        FluxzyDefault,

        FromUserStoreSerialNumber,

        /// <summary>
        /// From the current store, by thumbprint
        /// </summary>
        FromUserStoreThumbPrint,

        /// <summary>
        ///     Pfx and p12 files
        /// </summary>
        FromPkcs12
    }
}
