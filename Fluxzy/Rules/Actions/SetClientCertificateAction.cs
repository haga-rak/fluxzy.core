// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class SetClientCertificateAction : Action
    {
        public SetClientCertificateAction(Certificate clientCertificate)
        {
            ClientCertificate = clientCertificate;
        }

        public Certificate ClientCertificate { get; set; }

        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            context.ClientCertificates.Add(ClientCertificate.GetCertificate());
            return Task.CompletedTask;
        }
    }
}