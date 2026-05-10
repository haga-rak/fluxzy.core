// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class CertUninstallSettings : CommandSettings
    {
        [CommandArgument(0, "<cert-thumbprint>")]
        [Description("Certificate thumb print")]
        public string Thumbprint { get; set; } = null!;
    }

    public sealed class CertUninstallCommand : AsyncCommand<CertUninstallSettings>
    {
        protected override async Task<int> ExecuteAsync(CommandContext context, CertUninstallSettings settings,
            CancellationToken cancellationToken)
        {
            var certificateManager = new DefaultCertificateAuthorityManager();
            await certificateManager.RemoveCertificate(settings.Thumbprint);

            return 0;
        }
    }
}
