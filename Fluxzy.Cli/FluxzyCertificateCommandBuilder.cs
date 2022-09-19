// Copyright © 2022 Haga Rakotoharivelo

using System.CommandLine;
using System.IO;
using System.Linq;

namespace Fluxzy.Cli
{
    public class FluxzyCertificateCommandBuilder
    {
        public Command Build()
        {
            var command = new Command("cert");

            command.AddAlias("certificate");

            var exportCommand = BuildExportCommand();

            command.AddCommand(exportCommand);

            return command; 
        }

        private static Command BuildExportCommand()
        {
            var exportCommand = new Command("export", "Export the default embedded certificate used by fluxzy");

            var argumentFileInfo = new Argument<FileInfo>(
                name: "output-file",
                description: "The output file",
                parse: a => new FileInfo(a.Tokens.First().Value))
            {
                Arity = ArgumentArity.ExactlyOne
            };

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async (fileInfo) =>
            {
                await using var stream = fileInfo.Create();
                CertificateUtility.DumpDefaultCertificate(stream);
            }, argumentFileInfo);

            return exportCommand;
        }

        private static Command BuildCheckCommand()
        {
            var exportCommand = new Command("check", "Check if the provided certificate (or embedded if omit) is " +
                                                     "trusted");

            var argumentFileInfo = new Argument<FileInfo>(
                name: "cert-file",
                description: "Check if",
                parse: a => new FileInfo(a.Tokens.First().Value))
            {
                Arity = ArgumentArity.ZeroOrOne
            };

            exportCommand.AddArgument(argumentFileInfo);

            exportCommand.SetHandler(async (fileInfo) =>
            {
                await using var stream = fileInfo.Create();
                CertificateUtility.DumpDefaultCertificate(stream);
            }, argumentFileInfo);

            return exportCommand;
        }
    }
}