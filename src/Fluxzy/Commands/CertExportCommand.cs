// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class CertExportSettings : CommandSettings
    {
        [CommandArgument(0, "<output-file>")]
        [Description("The output file")]
        public string OutputFile { get; set; } = null!;
    }

    public sealed class CertExportCommand : AsyncCommand<CertExportSettings>
    {
        protected override async Task<int> ExecuteAsync(CommandContext context, CertExportSettings settings,
            CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(settings.OutputFile);
            await using var stream = fileInfo.Create();
            var certificateManager = new DefaultCertificateAuthorityManager();

            certificateManager.DumpDefaultCertificate(stream);

            return 0;
        }
    }
}
