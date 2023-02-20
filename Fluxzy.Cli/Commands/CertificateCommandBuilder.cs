// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

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

            exportCommand.SetHandler(async fileInfo =>
            {
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
                parse: argumentResult =>
                {
                    if (!argumentResult.Tokens.Any())
                        return null; 
                    
                    return new FileInfo(argumentResult.Tokens.First().Value);
                }) {
                Arity = ArgumentArity.ZeroOrOne
            };

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async fileInfo =>
            {
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

            exportCommand.SetHandler(async (fileInfo, console) =>
            {
                var certificate = FluxzySecurity.BuiltinCertificate;
                var certificateManager = new DefaultCertificateAuthorityManager();

                if (fileInfo != null)
                    certificate = new X509Certificate2(await File.ReadAllBytesAsync(fileInfo.FullName));

                if (certificateManager.IsCertificateInstalled(certificate.Thumbprint))
                    // ReSharper disable once LocalizableElement
                    console.WriteLine($"Trusted {certificate.SubjectName.Name}");
                else
                    console.Error.WriteLine($"NOT trusted {certificate.SubjectName.Name}");

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

            argumentFileInfo.SetDefaultValue("Embedded certificate");

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async (thumbPrint, console) =>
            {
                var certificateManager = new DefaultCertificateAuthorityManager();
                await certificateManager.RemoveCertificate(thumbPrint);

            }, argumentFileInfo, new ConsoleBinder());

            return exportCommand;
        }
    }
}