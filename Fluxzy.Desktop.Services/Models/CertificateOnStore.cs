// // Copyright 2022 - Haga Rakotoharivelo
// 

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