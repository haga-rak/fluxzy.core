// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Certificates;

namespace Fluxzy.Cli.Commands
{
    public class CertificateCommandBuilder
    {
        public Command Build()
        {
            var command = new Command("cert", "Manage root certificates used by the fluxzy");

            command.AddAlias("certificate");

            command.AddCommand(BuildExportCommand());
            command.AddCommand(BuildCheckCommand());
            command.AddCommand(BuildInstallCommand());
            command.AddCommand(BuildRemoveCommand());
            command.AddCommand(BuildListCommand());
            command.AddCommand(BuildCreateCommand());

            return command;
        }

        private static Command BuildExportCommand()
        {
            var exportCommand = new Command("export", "Export the default embedded certificate used by fluxzy");

            var argumentFileInfo = new Argument<FileInfo>(
                "output-file",
                description: "The output file",
                parse: a => new FileInfo(a.Tokens.First().Value)) {
                Arity = ArgumentArity.ExactlyOne
            };

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async fileInfo => {
                await using var stream = fileInfo.Create();
                var certificateManager = new DefaultCertificateAuthorityManager();

                certificateManager.DumpDefaultCertificate(stream);
            }, argumentFileInfo);

            return exportCommand;
        }

        private static Command BuildInstallCommand()
        {
            var exportCommand = new Command("install", "Trust a certificate as ROOT (need elevation)");

            var argumentFileInfo = new Argument<FileInfo?>(
                "cert-file",
                description: "A X509 certificate file or stdin if omitted",
                parse: argumentResult => {
                    if (!argumentResult.Tokens.Any()) {
                        return null;
                    }

                    return new FileInfo(argumentResult.Tokens.First().Value);
                }) {
                Arity = ArgumentArity.ZeroOrOne
            };

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async fileInfo => {
                var certificateManager = new DefaultCertificateAuthorityManager();
                X509Certificate2 certificate;

                if (fileInfo == null) {
                    // READ stdin to end 

                    var inputStream = Console.OpenStandardInput();

                    // We read a certificate up to 8K 
                    var buffer = new byte[8 * 1024];
                    var memoryStream = new MemoryStream(buffer);
                    await inputStream.CopyToAsync(memoryStream);

                    certificate = new X509Certificate2(buffer.AsSpan().Slice(0, (int) memoryStream.Position));
                }
                else {
                    certificate = new X509Certificate2(await File.ReadAllBytesAsync(fileInfo.FullName));
                }

                await certificateManager.InstallCertificate(certificate);
            }, argumentFileInfo);

            return exportCommand;
        }

        private static Command BuildCheckCommand()
        {
            var exportCommand = new Command("check", "Check if the provided certificate (or embedded if omit) is " +
                                                     "trusted");

            var argumentFileInfo = new Argument<FileInfo?>(
                "cert-file",
                description: "A X509 certificate file",
                parse: argument => new FileInfo(argument.Tokens.First().Value)) {
                Arity = ArgumentArity.ZeroOrOne
            };

            argumentFileInfo.SetDefaultValue("Embedded certificate");

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async (fileInfo, console) => {
                var certificate = FluxzySecurity.BuiltinCertificate;
                var certificateManager = new DefaultCertificateAuthorityManager();

                if (fileInfo != null) {
                    certificate = new X509Certificate2(await File.ReadAllBytesAsync(fileInfo.FullName));
                }

                if (certificateManager.IsCertificateInstalled(certificate))

                    // ReSharper disable once LocalizableElement
                {
                    console.WriteLine($"Trusted {certificate.SubjectName.Name}");
                }
                else {
                    console.Error.WriteLine($"NOT trusted {certificate.SubjectName.Name}");
                }
            }, argumentFileInfo, new ConsoleBinder());

            return exportCommand;
        }

        private static Command BuildRemoveCommand()
        {
            var exportCommand = new Command("uninstall", "Remove a certificate from Root CA authority store");

            var argumentFileInfo = new Argument<string>(
                "cert-thumbprint",
                description: "Certificate thumb print",
                parse: argument => argument.Tokens.First().Value) {
                Arity = ArgumentArity.ExactlyOne
            };

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async (thumbPrint, console) => {
                var certificateManager = new DefaultCertificateAuthorityManager();
                await certificateManager.RemoveCertificate(thumbPrint);
            }, argumentFileInfo, new ConsoleBinder());

            return exportCommand;
        }

        private static Command BuildListCommand()
        {
            var exportCommand = new Command("list", "List all root certificates");

            exportCommand.SetHandler(console => {
                var certificateManager = new DefaultCertificateAuthorityManager();

                foreach (var certificate in certificateManager.EnumerateRootCertificates()) {
                    console.Out.WriteLine($"{certificate.ThumbPrint}\t{certificate.Subject}");
                }

                return Task.CompletedTask;
            }, new ConsoleBinder());

            return exportCommand;
        }

        private static Command BuildCreateCommand()
        {
            var createCommand = new Command("create", "Create a self-signed root CA certificate in PKCS#12 format");

            var argumentFileInfo = new Argument<string>(
                "filePath",
                description: "Output path of the certificate",
                parse: argument => argument.Tokens.First().Value) {
                Arity = ArgumentArity.ExactlyOne
            };

            var argumentCn = new Argument<string>(
                "common-name",
                description: "Common name of the certificate",
                parse: argument => argument.Tokens.First().Value) {
                Arity = ArgumentArity.ExactlyOne
            };

            var validityOption = new Option<int>(
                new[] { "--validity", "-v" },
                description: "Validity of the certificate in days from now",
                getDefaultValue: () => 365 * 10) {
                Arity = ArgumentArity.ExactlyOne
            };

            var keySizeOption = new Option<int>(
                new[] { "--key-size", "-k" },
                description: "Key size of the certificate. Valid values are multiple of 1024 (max 16384)",
                parseArgument: r => {
                    var inputValueString = r.Tokens.FirstOrDefault()?.Value;

                    if (!int.TryParse(inputValueString, out var inputValue)) {
                        throw new ArgumentException(
                            $"Invalid key size {inputValueString}. Must be multiple of 1024 and less or equal than 16384.");
                    }

                    if (inputValue % 1024 != 0) {
                        throw new ArgumentException(
                            $"Invalid key size {inputValueString}. Must be multiple of 1024 and less or equal than 16384.");
                    }

                    if (inputValue > 16384 || inputValue < 1024) {
                        throw new ArgumentException(
                            $"Invalid key size {inputValueString}. Must be multiple of 1024 and less or equal than 16384.");
                    }

                    return inputValue;
                }) {
                Arity = ArgumentArity.ExactlyOne
            };

            // Build O, OU, L, ST, C options

            var passwordOption = new Option<string?>(
                new[] { "--password", "-p" },
                description: "Password for the created P12 file",
                getDefaultValue: () => null) {
                Arity = ArgumentArity.ExactlyOne
            };

            var oOption = new Option<string?>(
                new[] { "--O", "--o" },
                description: "Organization name",
                getDefaultValue: () => null) {
                Arity = ArgumentArity.ExactlyOne
            };

            var ouOption = new Option<string?>(
                new[] { "--OU", "--ou" },
                description: "Organization unit name",
                getDefaultValue: () => null) {
                Arity = ArgumentArity.ExactlyOne
            };

            var lOption = new Option<string?>(
                new[] { "--L", "--l" },
                description: "Locality name",
                getDefaultValue: () => null) {
                Arity = ArgumentArity.ExactlyOne
            };

            var stOption = new Option<string?>(
                new[] { "--ST", "--st" },
                description: "State or province name",
                getDefaultValue: () => null) {
                Arity = ArgumentArity.ExactlyOne
            };

            var cOption = new Option<string?>(
                new[] { "--C", "--c" },
                description: "Country name",
                getDefaultValue: () => null) {
                Arity = ArgumentArity.ExactlyOne
            };

            keySizeOption.SetDefaultValue(2048);

            createCommand.AddArgument(argumentFileInfo);
            createCommand.AddArgument(argumentCn);
            createCommand.AddOption(validityOption);
            createCommand.AddOption(keySizeOption);
            createCommand.AddOption(oOption);
            createCommand.AddOption(ouOption);
            createCommand.AddOption(lOption);
            createCommand.AddOption(stOption);
            createCommand.AddOption(cOption);
            createCommand.AddOption(passwordOption);

            createCommand.SetHandler(invocationContext => {
                var finalFileName =
                    invocationContext.ParseResult.GetValueForArgument(argumentFileInfo);

                var fileInfo = new FileInfo(finalFileName);

                var cCertificateBuilderOptions = new CertificateBuilderOptions(
                    invocationContext.ParseResult.GetValueForArgument(argumentCn)) {
                    Organization = invocationContext.Value<string>("O"),
                    OrganizationUnit = invocationContext.Value<string>("OU"),
                    Locality = invocationContext.Value<string>("L"),
                    State = invocationContext.Value<string>("ST"),
                    Country = invocationContext.Value<string>("C"),
                    DaysBeforeExpiration = invocationContext.Value<int>("validity"),
                    KeySize = invocationContext.Value<int>("key-size"),
                    P12Password = invocationContext.Value<string>("password")
                };

                var certificateBuilder = new CertificateBuilder(cCertificateBuilderOptions);
                var result = certificateBuilder.CreateSelfSigned();

                fileInfo.Directory?.Create();
                File.WriteAllBytes(fileInfo.FullName, result);
            });

            return createCommand;
        }
    }
}
