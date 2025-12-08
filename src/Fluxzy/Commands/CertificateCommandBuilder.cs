// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core;

namespace Fluxzy.Cli.Commands
{
    public class CertificateCommandBuilder
    {
        public Command Build(EnvironmentProvider environmentProvider)
        {
            var command = new Command("cert", "Manage root certificates used by the fluxzy");

            command.Aliases.Add("certificate");

            command.Subcommands.Add(BuildExportCommand());
            command.Subcommands.Add(BuildCheckCommand());
            command.Subcommands.Add(BuildInstallCommand());
            command.Subcommands.Add(BuildRemoveCommand());
            command.Subcommands.Add(BuildListCommand());
            command.Subcommands.Add(BuildCreateCommand());
            command.Subcommands.Add(BuildDefaultCommand(environmentProvider));

            return command;
        }

        private static Command BuildExportCommand()
        {
            var exportCommand = new Command("export", "Export the default embedded certificate used by fluxzy");
            
            var argumentFileInfo = new Argument<FileInfo>("output-file") {
                Description = "The output file",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = a => new FileInfo(a.Tokens.First().Value)
            };

            exportCommand.Arguments.Add(argumentFileInfo);
            exportCommand.SetAction(async (parseResult, u) => {
                
                await using var stream = parseResult.GetRequiredValue<FileInfo>(argumentFileInfo)
                                                    .Create();
                var certificateManager = new DefaultCertificateAuthorityManager();

                certificateManager.DumpDefaultCertificate(stream);
            });
            
            return exportCommand;
        }

        private static Command BuildInstallCommand()
        {
            var exportCommand = new Command("install", "Trust a certificate as ROOT (need elevation)");
            var argumentFileInfo = new Argument<FileInfo?>(
                "cert-file") {
                Description = "A X509 certificate file or stdin if omitted",
                Arity = ArgumentArity.ZeroOrOne,
                CustomParser = argumentResult => {
                    if (!argumentResult.Tokens.Any()) {
                        return null;
                    }

                    return new FileInfo(argumentResult.Tokens.First().Value);
                }
            };

            exportCommand.Arguments.Add(argumentFileInfo);

            exportCommand.SetAction(async (parseResult, cancellationToken) => {
                var certificateManager = new DefaultCertificateAuthorityManager();
                X509Certificate2 certificate;
                var fileInfo = parseResult.GetValue(argumentFileInfo);

                if (fileInfo == null) {
                    // READ stdin to end 

                    var inputStream = Console.OpenStandardInput();

                    // We read a certificate up to 8K 
                    var buffer = new byte[8 * 1024];
                    var memoryStream = new MemoryStream(buffer);
                    await inputStream.CopyToAsync(memoryStream, cancellationToken);

                    certificate = new X509Certificate2(buffer.AsSpan().Slice(0, (int) memoryStream.Position));
                }
                else {
                    certificate = new X509Certificate2(await File.ReadAllBytesAsync(fileInfo.FullName, cancellationToken));
                }

                await certificateManager.InstallCertificate(certificate);
            });

            return exportCommand;
        }

        private static Command BuildCheckCommand()
        {
            var exportCommand = new Command("check", "Check if the provided certificate (or embedded if omit) is " +
                                                     "trusted");

            var argumentFileInfo = new Argument<FileInfo?>(
                "cert-file") {
                Description = "A X509 certificate file",
                Arity = ArgumentArity.ZeroOrOne,
                CustomParser = argument => new FileInfo(argument.Tokens.First().Value)
            };

            exportCommand.Arguments.Add(argumentFileInfo);

            exportCommand.SetAction(async (parseResult, cancellationToken) => {
                var fileInfo = parseResult.GetValue(argumentFileInfo);
                var certificate = fileInfo != null ? 
                    new X509Certificate2(await File.ReadAllBytesAsync(fileInfo.FullName, cancellationToken)) 
                    : FluxzySecurityParams.Current.BuiltinCertificate;

                var certificateManager = new DefaultCertificateAuthorityManager();

                if (certificateManager.IsCertificateInstalled(certificate)){
                    Console.WriteLine($"Trusted {certificate.SubjectName.Name}");
                }
                else {
                    throw new Exception($"NOT trusted {certificate.SubjectName.Name}");
                }
            });

            return exportCommand;
        }

        private static Command BuildRemoveCommand()
        {
            var exportCommand = new Command("uninstall", "Remove a certificate from Root CA authority store");

            var argumentFileInfo = new Argument<string>(
                "cert-thumbprint") {
                Description = "Certificate thumb print",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = argument => argument.Tokens.First().Value
            };

            exportCommand.Arguments.Add(argumentFileInfo);

            exportCommand.SetAction(async (parseResult, cancellationToken) => {
                var thumbPrint = parseResult.GetRequiredValue(argumentFileInfo);
                var certificateManager = new DefaultCertificateAuthorityManager();
                await certificateManager.RemoveCertificate(thumbPrint);
            });

            return exportCommand;
        }

        private static Command BuildListCommand()
        {
            var exportCommand = new Command("list", "List all root certificates");

            exportCommand.SetAction((parseResult, cancellationToken) => {
                var certificateManager = new DefaultCertificateAuthorityManager();

                foreach (var certificate in certificateManager.EnumerateRootCertificates()) {
                    Console.WriteLine($"{certificate.ThumbPrint}\t{certificate.Subject}");
                }

                return Task.CompletedTask;
            });

            return exportCommand;
        }

        private static Command BuildCreateCommand()
        {
            var createCommand = new Command("create", "Create a self-signed root CA certificate in PKCS#12 format");

            var argumentFileInfo = new Argument<string>(
                "filePath") {
                Description = "Output path of the certificate",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = argument => argument.Tokens.First().Value
            };

            var argumentCn = new Argument<string>(
                "common-name") {
                Description = "Common name of the certificate",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = argument => argument.Tokens.First().Value
            };

            var validityOption = new Option<int>(
                "--validity") {
                Description = "Validity of the certificate in days from now",
                Arity = ArgumentArity.ExactlyOne,
                DefaultValueFactory = _ => 365 * 10
            };
            validityOption.Aliases.Add("-v");

            var keySizeOption = new Option<int>(
                "--key-size") {
                Description = "Key size of the certificate. Valid values are multiple of 1024 (max 16384)",
                Arity = ArgumentArity.ExactlyOne,
                DefaultValueFactory = _ => 2048,
                CustomParser = r => {
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
                }
            };
            keySizeOption.Aliases.Add("-k");

            // Build O, OU, L, ST, C options

            var passwordOption = new Option<string?>(
                "--password") {
                Description = "Password for the created P12 file",
                Arity = ArgumentArity.ExactlyOne
            };
            passwordOption.Aliases.Add("-p");

            var oOption = new Option<string?>(
                "--O") {
                Description = "Organization name",
                Arity = ArgumentArity.ExactlyOne
            };
            oOption.Aliases.Add("--o");

            var ouOption = new Option<string?>(
                "--OU") {
                Description = "Organization unit name",
                Arity = ArgumentArity.ExactlyOne
            };
            ouOption.Aliases.Add("--ou");

            var lOption = new Option<string?>(
                "--L") {
                Description = "Locality name",
                Arity = ArgumentArity.ExactlyOne
            };
            lOption.Aliases.Add("--l");

            var stOption = new Option<string?>(
                "--ST") {
                Description = "State or province name",
                Arity = ArgumentArity.ExactlyOne
            };
            stOption.Aliases.Add("--st");

            var cOption = new Option<string?>(
                "--C") {
                Description = "Country name",
                Arity = ArgumentArity.ExactlyOne
            };
            cOption.Aliases.Add("--c");

            createCommand.Arguments.Add(argumentFileInfo);
            createCommand.Arguments.Add(argumentCn);
            createCommand.Options.Add(validityOption);
            createCommand.Options.Add(keySizeOption);
            createCommand.Options.Add(oOption);
            createCommand.Options.Add(ouOption);
            createCommand.Options.Add(lOption);
            createCommand.Options.Add(stOption);
            createCommand.Options.Add(cOption);
            createCommand.Options.Add(passwordOption);

            createCommand.SetAction((parseResult, cancellationToken) => {
                var finalFileName = parseResult.GetRequiredValue(argumentFileInfo);

                var fileInfo = new FileInfo(finalFileName);

                var cCertificateBuilderOptions = new CertificateBuilderOptions(
                    parseResult.GetRequiredValue(argumentCn)) {
                    Organization = parseResult.GetValue(oOption),
                    OrganizationUnit = parseResult.GetValue(ouOption),
                    Locality = parseResult.GetValue(lOption),
                    State = parseResult.GetValue(stOption),
                    Country = parseResult.GetValue(cOption),
                    DaysBeforeExpiration = parseResult.GetValue(validityOption),
                    KeySize = parseResult.GetValue(keySizeOption),
                    P12Password = parseResult.GetValue(passwordOption)
                };

                var certificateBuilder = new CertificateBuilder(cCertificateBuilderOptions);
                var result = certificateBuilder.CreateSelfSigned();

                fileInfo.Directory?.Create();
                File.WriteAllBytes(fileInfo.FullName, result);

                return Task.CompletedTask;
            });

            return createCommand;
        }


        private static Command BuildDefaultCommand(EnvironmentProvider environmentProvider)
        {
            var setDefaultCommand = new Command("default",
                "Get or set the default root CA for the current user. Environment variable FLUXZY_ROOT_CERTIFICATE overrides this setting.");

            var argumentFileInfo = new Argument<string?>(
                "pkcs12-certificate") {
                Description = "",
                Arity = ArgumentArity.ZeroOrOne,
                CustomParser = argument => argument.Tokens.First().Value
            };

            setDefaultCommand.Arguments.Add(argumentFileInfo);

            setDefaultCommand.SetAction(async (parseResult, cancellationToken) => {
                var defaultCertificatePath = parseResult.GetValue(argumentFileInfo);

                if (defaultCertificatePath == null) {
                    // Print default certificate 
                    var certificate = new FluxzySecurity(FluxzySecurity.DefaultCertificatePath, environmentProvider)
                        .BuiltinCertificate;

                    Console.WriteLine(certificate.ToString(true));
                    return;
                }

                var certificateFileInfo = new FileInfo(defaultCertificatePath);

                if (!certificateFileInfo.Exists) {
                    throw new FileNotFoundException($"The certificate file does not exist " +
                                                    $"`{certificateFileInfo.FullName}`", certificateFileInfo.FullName);
                }

                var certificateContent = await File.ReadAllBytesAsync(certificateFileInfo.FullName, cancellationToken);

                try
                {
                    using var newCertificate = new X509Certificate2(certificateContent);
                    var hasPk = newCertificate.HasPrivateKey;

                    if (!hasPk) {
                        throw new InvalidOperationException("The provided certificate must have a private key");
                    }
                }
                catch (CryptographicException tex) {
                    // We allow invalid password 
                    if (tex.HResult != -2146233087) {
                        throw new InvalidOperationException("The provided file is not a valid PKCS#12 certificate");
                    }
                    else {
                        Console.WriteLine(@"Warning: The provided certificate has been added but needs a passphrase. " +
                                          @"Consider passing passphrase through" +
                                          @" FLUXZY_ROOT_CERTIFICATE_PASSWORD environment variable.");
                    }
                }

                Console.WriteLine("The default certificate has been changed.");
                
                FluxzySecurity.SetDefaultCertificateForUser(
                    certificateContent, environmentProvider,
                    FluxzySecurity.DefaultCertificatePath);

            });

            return setDefaultCommand;
        }
    }
}
