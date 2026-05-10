// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class CertCheckSettings : CommandSettings
    {
        [CommandArgument(0, "[cert-file]")]
        [Description("A X509 certificate file")]
        public string? CertFile { get; set; }
    }

    public sealed class CertCheckCommand : AsyncCommand<CertCheckSettings>
    {
        private readonly IFluxzyConsole _console;

        public CertCheckCommand(IFluxzyConsole console)
        {
            _console = console;
        }

        protected override async Task<int> ExecuteAsync(CommandContext context, CertCheckSettings settings,
            CancellationToken cancellationToken)
        {
            X509Certificate2 certificate = string.IsNullOrEmpty(settings.CertFile)
                ? FluxzySecurityParams.Current.BuiltinCertificate
                : new X509Certificate2(await File.ReadAllBytesAsync(settings.CertFile, cancellationToken));

            var certificateManager = new DefaultCertificateAuthorityManager();

            if (certificateManager.IsCertificateInstalled(certificate)) {
                _console.Out.Write($"Trusted {certificate.SubjectName.Name}" + Environment.NewLine);

                return 0;
            }

            throw new Exception($"NOT trusted {certificate.SubjectName.Name}");
        }
    }
}
