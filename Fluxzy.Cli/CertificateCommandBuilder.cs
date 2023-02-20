﻿// Copyright © 2022 Haga Rakotoharivelo

using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Cli
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
                var certificateManager = new CertificateAuthorityManager(); 
                
                certificateManager.DumpDefaultCertificate(stream);
            }, argumentFileInfo);

            return exportCommand;
        }

        private static Command BuildInstallCommand()
        {
            var exportCommand = new Command("install", "Trust a certificate as ROOT (need elevation)");

            var argumentFileInfo = new Argument<FileInfo>(
                "cert-file",
                description: "A X509 certificate file",
                parse: a => new FileInfo(a.Tokens.First().Value)) {
                Arity = ArgumentArity.ExactlyOne
            };

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async fileInfo =>
            {
                var certificate = new X509Certificate2(await File.ReadAllBytesAsync(fileInfo.FullName));
                var certificateManager = new CertificateAuthorityManager(); 
                certificateManager.InstallCertificate(certificate);
            }, argumentFileInfo);

            return exportCommand;
        }

        private static Command BuildCheckCommand()
        {
            var exportCommand = new Command("check", "Check if the provided certificate (or embedded if omit) is " +
                                                     "trusted");

            var argumentFileInfo = new Argument<FileInfo>(
                "cert-file",
                description: "A X509 certificate file",
                parse: a => new FileInfo(a.Tokens.First().Value)) {
                Arity = ArgumentArity.ZeroOrOne
            };

            argumentFileInfo.SetDefaultValue("Embedded certificate");

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async (fileInfo, console) =>
            {
                var certificate = FluxzySecurity.BuiltinCertificate;
                var certificateManager = new CertificateAuthorityManager();

                if (fileInfo != null)
                    certificate = new X509Certificate2(await File.ReadAllBytesAsync(fileInfo.FullName));

                if (certificateManager.IsCertificateInstalled(certificate.SerialNumber))
                    // ReSharper disable once LocalizableElement
                    console.WriteLine($"Trusted {certificate.SubjectName.Name}");
                else
                    console.Error.WriteLine($"NOT trusted {certificate.SubjectName.Name}");

            }, argumentFileInfo, new ConsoleBinder());

            return exportCommand;
        }
    }
}