// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services.Models
{
    public class CertificateOnStore
    {
        public CertificateOnStore(string thumbPrint, string friendlyName)
        {
            ThumbPrint = thumbPrint;
            FriendlyName = friendlyName;
        }

        public string ThumbPrint { get; }

        public string FriendlyName { get; }
    }
}
