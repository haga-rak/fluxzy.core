// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Certificates
{
    public enum CertificateRetrieveMode
    {
        FluxzyDefault,

        FromUserStoreSerialNumber,

        FromUserStoreThumbPrint,

        /// <summary>
        ///     Typicaly pfx and p12 files
        /// </summary>
        FromPkcs12
    }
}
