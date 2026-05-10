// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class CertCreateSettings : CommandSettings
    {
        [CommandArgument(0, "<filePath>")]
        [Description("Output path of the certificate")]
        public string FilePath { get; set; } = null!;

        [CommandArgument(1, "<common-name>")]
        [Description("Common name of the certificate")]
        public string CommonName { get; set; } = null!;

        [CommandOption("-v|--validity")]
        [Description("Validity of the certificate in days from now")]
        [DefaultValue(365 * 10)]
        public int Validity { get; set; } = 365 * 10;

        [CommandOption("-k|--key-size")]
        [Description("Key size of the certificate. Valid values are multiple of 1024 (max 16384)")]
        [DefaultValue(2048)]
        public int KeySize { get; set; } = 2048;

        // Spectre 0.55.0 rejects single-character long-form options (--O, --L, --C). We accept
        // them via the short forms below, and FluxzyStartup.RewriteSpectreArgs rewrites the
        // legacy "--O"/"--L"/"--C" tokens to "-O"/"-L"/"-C" before Spectre sees them.
        [CommandOption("-O|-o")]
        [Description("Organization name")]
        public string? Organization { get; set; }

        [CommandOption("--OU|--ou")]
        [Description("Organization unit name")]
        public string? OrganizationUnit { get; set; }

        [CommandOption("-L|-l")]
        [Description("Locality name")]
        public string? Locality { get; set; }

        [CommandOption("--ST|--st")]
        [Description("State or province name")]
        public string? State { get; set; }

        [CommandOption("-C|-c")]
        [Description("Country name")]
        public string? Country { get; set; }

        [CommandOption("-p|--password")]
        [Description("Password for the created P12 file")]
        public string? Password { get; set; }

        public override Spectre.Console.ValidationResult Validate()
        {
            if (KeySize % 1024 != 0 || KeySize < 1024 || KeySize > 16384) {
                return Spectre.Console.ValidationResult.Error(
                    $"Invalid key size {KeySize}. Must be multiple of 1024 and less or equal than 16384.");
            }

            return base.Validate();
        }
    }

    public sealed class CertCreateCommand : Command<CertCreateSettings>
    {
        protected override int Execute(CommandContext context, CertCreateSettings settings,
            CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(settings.FilePath);

            var options = new CertificateBuilderOptions(settings.CommonName) {
                Organization = settings.Organization,
                OrganizationUnit = settings.OrganizationUnit,
                Locality = settings.Locality,
                State = settings.State,
                Country = settings.Country,
                DaysBeforeExpiration = settings.Validity,
                KeySize = settings.KeySize,
                P12Password = settings.Password
            };

            var certificateBuilder = new CertificateBuilder(options);
            var result = certificateBuilder.CreateSelfSigned();

            fileInfo.Directory?.Create();
            File.WriteAllBytes(fileInfo.FullName, result);

            return 0;
        }
    }
}
