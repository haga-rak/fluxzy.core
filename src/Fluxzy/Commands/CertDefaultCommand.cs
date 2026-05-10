// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class CertDefaultSettings : CommandSettings
    {
        [CommandArgument(0, "[pkcs12-certificate]")]
        [Description("")]
        public string? Pkcs12Certificate { get; set; }
    }

    public sealed class CertDefaultCommand : AsyncCommand<CertDefaultSettings>
    {
        private readonly IFluxzyConsole _console;
        private readonly EnvironmentProvider _environmentProvider;

        public CertDefaultCommand(IFluxzyConsole console, EnvironmentProvider environmentProvider)
        {
            _console = console;
            _environmentProvider = environmentProvider;
        }

        protected override async Task<int> ExecuteAsync(CommandContext context, CertDefaultSettings settings,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings.Pkcs12Certificate)) {
                var certificate = new FluxzySecurity(FluxzySecurity.DefaultCertificatePath, _environmentProvider)
                    .BuiltinCertificate;

                _console.Out.Write(certificate.ToString(true) + Environment.NewLine);

                return 0;
            }

            var certificateFileInfo = new FileInfo(settings.Pkcs12Certificate);

            if (!certificateFileInfo.Exists) {
                throw new FileNotFoundException(
                    $"The certificate file does not exist `{certificateFileInfo.FullName}`",
                    certificateFileInfo.FullName);
            }

            var certificateContent = await File.ReadAllBytesAsync(certificateFileInfo.FullName, cancellationToken);

            try {
                using var newCertificate = new X509Certificate2(certificateContent);
                var hasPk = newCertificate.HasPrivateKey;

                if (!hasPk) {
                    throw new InvalidOperationException("The provided certificate must have a private key");
                }
            }
            catch (CryptographicException tex) {
                if (tex.HResult != -2146233087) {
                    throw new InvalidOperationException("The provided file is not a valid PKCS#12 certificate");
                }

                _console.Out.Write(
                    @"Warning: The provided certificate has been added but needs a passphrase. " +
                    @"Consider passing passphrase through" +
                    @" FLUXZY_ROOT_CERTIFICATE_PASSWORD environment variable." + Environment.NewLine);
            }

            _console.Out.Write("The default certificate has been changed." + Environment.NewLine);

            FluxzySecurity.SetDefaultCertificateForUser(
                certificateContent, _environmentProvider,
                FluxzySecurity.DefaultCertificatePath);

            return 0;
        }
    }
}
