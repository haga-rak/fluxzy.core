// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using Fluxzy.Clients.Mock;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core.Breakpoints;
using YamlDotNet.Serialization;
using Fluxzy.Core;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata("Reply with the default root certificate used by fluxzy")]
    public class MountCertificateAuthorityAction : Action
    {
        public override FilterScope ActionScope =>  InternalScope;

        [JsonIgnore]
        [YamlIgnore]
        internal FilterScope InternalScope { get; set; } = FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Reply with CA";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (context.FluxzySetting?.CaCertificate != null) {
                var certificate = context.FluxzySetting.CaCertificate.GetX509Certificate();

                var bodyContent = BodyContent.CreateFromArray(certificate.ExportToPem(), "application/x-x509-ca-cert");
                var mockedResponse = new MockedResponseContent(200,
                    bodyContent);

                mockedResponse.Headers.Add("Content-Disposition", "attachment; filename=\"ca.crt\"");

                context.PreMadeResponse = mockedResponse; 
            }

            return default;
        }
    }
}
