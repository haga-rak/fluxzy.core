// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata(
        "Use a specific server certificate. Certificate can be retrieved from user store or from a PKCS12 file")]
    public class UseCertificateAction : Action
    {
        public UseCertificateAction(Certificate clientCertificate)
        {
#pragma warning disable CS0618
            clientCertificate ??= new Certificate
#pragma warning restore CS0618
            {
                RetrieveMode = CertificateRetrieveMode.FromPkcs12
            };

            ServerCertificate = clientCertificate;
        }

        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        [ActionDistinctive(Expand = true, Description = "Server certificate")]
        public Certificate ServerCertificate { get; set; }

        public override string DefaultDescription => $"Use certificate".Trim();

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.ServerCertificate = ServerCertificate.GetX509Certificate();
            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Use a certificate with serial number `xxxxxx` retrieved from for local user " +
                "as a server certificate",
                new UseCertificateAction(new Certificate
                {
                    RetrieveMode = CertificateRetrieveMode.FromUserStoreSerialNumber,
                    SerialNumber = "xxxxxx"
                }));
        }
    }
}
