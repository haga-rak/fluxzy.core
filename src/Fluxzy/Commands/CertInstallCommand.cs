// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class CertInstallSettings : CommandSettings
    {
        [CommandArgument(0, "[cert-file]")]
        [Description("A X509 certificate file or stdin if omitted")]
        public string? CertFile { get; set; }
    }

    public sealed class CertInstallCommand : AsyncCommand<CertInstallSettings>
    {
        protected override async Task<int> ExecuteAsync(CommandContext context, CertInstallSettings settings,
            CancellationToken cancellationToken)
        {
            var certificateManager = new DefaultCertificateAuthorityManager();
            X509Certificate2 certificate;

            if (string.IsNullOrEmpty(settings.CertFile)) {
                var inputStream = Console.OpenStandardInput();

                var buffer = new byte[8 * 1024];
                var memoryStream = new MemoryStream(buffer);
                await inputStream.CopyToAsync(memoryStream, cancellationToken);

                certificate = new X509Certificate2(buffer.AsSpan().Slice(0, (int) memoryStream.Position));
            }
            else {
                certificate = new X509Certificate2(await File.ReadAllBytesAsync(settings.CertFile, cancellationToken));
            }

            await certificateManager.InstallCertificate(certificate);

            return 0;
        }
    }
}
