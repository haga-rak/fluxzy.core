// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy
{
    public enum CertificateRetrieveMode
    {
        FluxzyDefault,

        FromUserStoreByThumbPrint, 

        /// <summary>
        /// Typicaly pfx and p12 files
        /// </summary>
        FromPkcs12
    }
}