// Copyright © 2022 Haga Rakotoharivelo

using System.Security.Authentication;
using Fluxzy.Rules.Actions;
using Action = Fluxzy.Rules.Action;

namespace Fluxzy.Desktop.Services.Rules
{
    public class ActionTemplateManager
    {
        private readonly List<Action> _defaultActions = new()
        {
            new AddRequestHeaderAction(string.Empty, string.Empty),
            new AddResponseHeaderAction(string.Empty, string.Empty),
            new UpdateRequestHeaderAction("", ""),
            new UpdateResponseHeaderAction("", ""),
            new DeleteRequestHeaderAction(""),
            new DeleteResponseHeaderAction(""),
            new ApplyCommentAction(""),
            new ApplyTagAction(),
            new ChangeRequestMethodAction("GET"),
            new ChangeRequestPathAction("/"),
            new SkipSslTunnelingAction(),
            new SetClientCertificateAction(new Certificate
            {
                RetrieveMode = CertificateRetrieveMode.FromPkcs12
            }),
            new SpoofDnsAction(),
            new ForceHttp11Action(),
            new ForceHttp2Action(),
            new ForceTlsVersion(SslProtocols.Tls13)
        };

        public List<Action> GetDefaultActions()
        {
            return _defaultActions;
        }
    }
}
