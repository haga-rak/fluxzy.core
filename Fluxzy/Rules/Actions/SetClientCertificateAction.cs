// Copyright © 2022 Haga Rakotoharivelo

using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Add a client certificate to the exchange. The client certificate can be retrieved from the default store (my) or from a PKCS#12 file (.p12, pfx)
    /// The actual certificate is not stored in any static fluxzy settings and, therefore, must be available at runtime. 
    /// </summary>
    [ActionMetadata("Add a client certificate to the exchange. The client certificate can be retrieved from the default store (my) or from a PKCS#12 file (.p12, pfx). <br/>" +
                    "The actual certificate is not stored in any static fluxzy settings and, therefore, must be available at runtime. ")]
    public class SetClientCertificateAction : Action
    {
        public SetClientCertificateAction(Certificate?  clientCertificate)
        {
            clientCertificate ??= new Certificate()
            {
                RetrieveMode = CertificateRetrieveMode.FromPkcs12
            }; 
            
            ClientCertificate = clientCertificate;
        }

        /// <summary>
        /// The certificate information
        /// </summary>
        public Certificate ClientCertificate { get; set; }

        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            if (ClientCertificate != null)
            {
                context.ClientCertificates ??= new X509Certificate2Collection();
                context.ClientCertificates.Add(ClientCertificate.GetCertificate());
            }

            return default;
        }

        public override string DefaultDescription => $"Set client certificate".Trim();
    }
}