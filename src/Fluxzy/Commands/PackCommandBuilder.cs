// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fluxzy.Cli.Commands
{
    public class PackCommandBuilder
    {
        public Command Build()
        {
            var command = new Command("pack", "Export a fluxzy result directory to a specific archive format");

            var inputDirectoryArgument = BuildInputDirectoryOption();
            var outputFileArgument = BuildOutputFileArgument();
            var outputFileFormatOption = BuildOutputFileFormatOption();

            command.Arguments.Add(inputDirectoryArgument);
            command.Arguments.Add(outputFileArgument);
            command.Options.Add(outputFileFormatOption);

            command.SetAction(async (parseResult, cancellationToken) => {
                var directory = parseResult.GetRequiredValue(inputDirectoryArgument);
                var outputFile = parseResult.GetRequiredValue(outputFileArgument);
                var format = parseResult.GetValue(outputFileFormatOption);

                if (!directory.Exists)
                    throw new InvalidOperationException($"Directory does not exists {directory.FullName}");

                outputFile.Directory?.Create();

                var packager = format == null!
                    ? PackagerRegistry.Instance.InferPackagerFromFileName(outputFile.Name)
                    : PackagerRegistry.Instance.GetPackageOrDefault(format);

                using var outputFileStream = outputFile.Create();

                await packager.Pack(directory.FullName, outputFileStream, null);
            });

            return command;
        }

        private static Argument<DirectoryInfo> BuildInputDirectoryOption()
        {
            var argument = new Argument<DirectoryInfo>(
                "input-directory") {
                Description = "a fluxzy folder result to export",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = t => new DirectoryInfo(t.Tokens.First().Value)
            };

            return argument;
        }

        private static Argument<FileInfo> BuildOutputFileArgument()
        {
            var argument = new Argument<FileInfo>(
                "output-file") {
                Description = "a fluxzy folder result to export",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = t => new FileInfo(t.Tokens.First().Value)
            };

            return argument;
        }

        private static Option<string> BuildOutputFileFormatOption()
        {
            var option = new Option<string>(
                "-f") {
                Description = "The output file format among fluxzy, har and saz",
                Arity = ArgumentArity.ExactlyOne
            };

            option.Aliases.Add("--format");

            return option;
        }
    }
}
