// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class SetClientCertificateAction : IAction
    {
        public SetClientCertificateAction(Certificate clientCertificate)
        {
            ClientCertificate = clientCertificate;
        }

        public Certificate ClientCertificate { get; set; }

        public FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            context.ClientCertificates.Add(ClientCertificate.GetCertificate());
            return Task.CompletedTask;
        }
    }
}