// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.CommandLine;
using System.IO;
using System.Linq;

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

            command.Add(inputDirectoryArgument);
            command.Add(outputFileArgument);
            command.Add(outputFileFormatOption);

            command.SetHandler(async (directory, outputFile, format) => {
                if (!directory.Exists)
                    throw new InvalidOperationException($"Directory does not exists {directory.FullName}");

                outputFile.Directory?.Create();

                var packager = format == null!
                    ? PackagerRegistry.Instance.InferPackagerFromFileName(outputFile.Name)
                    : PackagerRegistry.Instance.GetPackageOrDefault(format);

                using var outputFileStream = outputFile.Create();

                await packager.Pack(directory.FullName, outputFileStream, null);
            }, inputDirectoryArgument, outputFileArgument, outputFileFormatOption);

            return command;
        }

        private static Argument<DirectoryInfo> BuildInputDirectoryOption()
        {
            var argument = new Argument<DirectoryInfo>(
                "input-directory",
                description: "a fluxzy folder result to export",
                parse: t => new DirectoryInfo(t.Tokens.First().Value)) {
                Arity = ArgumentArity.ExactlyOne
            };

            return argument;
        }

        private static Argument<FileInfo> BuildOutputFileArgument()
        {
            var argument = new Argument<FileInfo>(
                "output-file",
                description: "a fluxzy folder result to export",
                parse: t => new FileInfo(t.Tokens.First().Value)) {
                Arity = ArgumentArity.ExactlyOne
            };

            return argument;
        }

        private static Option<string> BuildOutputFileFormatOption()
        {
            var option = new Option<string>(
                "f",
                description: "The output file format among fluxzy and saz",
                getDefaultValue: () => null!) {
                Arity = ArgumentArity.ExactlyOne
            };

            option.AddAlias("--format");

            return option;
        }
    }
}
