// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-r

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands.Dissects;
using Fluxzy.Cli.Commands.Dissects.Formatters;
using Fluxzy.Core.Pcap.Pcapng.Merge;
using Fluxzy.Readers;

namespace Fluxzy.Cli.Commands
{
    public class DissectCommandBuilder
    {
        public Command Build()
        {
            var command = new Command("dissect",
                "Read content of a previously captured file or directory.");

            command.Aliases.Add("dis");
            
            var archiveReaderArgument = CreateInputFileOrDirectoryArgument(); 

            command.Subcommands.Add(BuildExportPcapCommand());

            command.Options.Add(CreateExchangeIdsOption());
            command.Options.Add(CreateFormatOption());
            command.Options.Add(CreateOutputFileOption());
            command.Options.Add(CreateUniqueOption());
            command.Arguments.Add(archiveReaderArgument);

            command.SetAction(async (parseResult, cancellationToken) => {
                await Run(parseResult, archiveReaderArgument);
            });

            return command;
        }

        private static Command BuildExportPcapCommand()
        {
            var exportCommand = new Command("pcap", "Export pcapng files from an archive or dump directory.");

            var inputFileOrDirectory = new Argument<string>(
                "input-file-or-directory") {
                Description = "Input file or directory",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = a => a.Tokens.First().Value
            };
            
            var outFileOption = new Option<FileInfo>(
                "--output-file") {
                Description = "Output file",
                Arity = ArgumentArity.ExactlyOne,
                Required = true,
                CustomParser = a => new FileInfo(a.Tokens.First().Value)
            };

            outFileOption.Aliases.Add("-o");

            exportCommand.Arguments.Add(inputFileOrDirectory);
            exportCommand.Options.Add(outFileOption);

            exportCommand.SetAction((parseResult, cancellationToken) => {
                var fileOrDirectory = parseResult.GetRequiredValue(inputFileOrDirectory);
                var outFile = parseResult.GetRequiredValue(outFileOption);

                if (File.Exists(fileOrDirectory)) {
                    using var outStream = File.Create(outFile.FullName);

                    PcapMerge.MergeArchive(fileOrDirectory,
                        outStream);

                    return Task.CompletedTask; 
                }

                if (Directory.Exists(fileOrDirectory)) {
                    using var outStream = File.Create(outFile.FullName);

                    PcapMerge.MergeDumpDirectory(fileOrDirectory,
                        outStream);

                    return Task.CompletedTask; 
                }

                throw new InvalidOperationException($"{fileOrDirectory} is neither a file or a directory");
            });

            return exportCommand;
        }


        private async Task Run(ParseResult parseResult, Argument<IArchiveReader> archiveReaderArgument)
        {
            var stdout = parseResult.InvocationConfiguration?.Output ?? Console.Out;

            var exchangeIds = parseResult.Value<List<int>?>("id");
            var format = parseResult.Value<string>("format");
            var outputFile = parseResult.Value<FileInfo?>("output-file");
            var mustBeUnique = parseResult.Value<bool>("unique");

            var archiveReader = parseResult.GetRequiredValue(archiveReaderArgument);

            var dissectionOptions = new DissectionOptions(mustBeUnique, exchangeIds?.ToHashSet(), format);
            var flowManager = new DissectionFlowManager(new SequentialFormatter(),
                FormatterRegistration.Formatters);

            outputFile?.Directory?.Create();

            Stream? outputFileStream = null;

            try {
                var stdoutStream = outputFile != null 
                    ? (outputFileStream = outputFile.Create()) 
                    : Console.OpenStandardOutput();

                var stdErrStream = Console.OpenStandardError();

                var result = await flowManager.Apply(archiveReader, stdoutStream, stdErrStream, dissectionOptions);

                if (!result)
                    Environment.ExitCode = 1;
            }
            finally {
                // ReSharper disable once MethodHasAsyncOverload
                outputFileStream?.Dispose();
            }
        }

        private static Argument<IArchiveReader> CreateInputFileOrDirectoryArgument()
        {
            var argument = new Argument<IArchiveReader>(
                  "input-file-or-directory") {
                Description = "A fluxzy file or directory to dissect",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = t => {
                    var inputFileOrDirectory = t.Tokens.First().Value;

                    try {
                        if (Directory.Exists(inputFileOrDirectory)) {
                            return new DirectoryArchiveReader(inputFileOrDirectory);
                        }

                        if (File.Exists(inputFileOrDirectory)) {
                            return new FluxzyArchiveReader(inputFileOrDirectory);
                        }

                        t.AddError($"Input file or directory \"{inputFileOrDirectory}\" does not exists");
                    }
                    catch (Exception ex) {
                        t.AddError($"Cannot read {inputFileOrDirectory} : {ex.Message}");
                    }

                    return null!;
                }
            };

            return argument;
        }


        private static Option<List<int>?> CreateExchangeIdsOption()
        {
            var option = new Option<List<int>?>(
                "--id") {
                Description = "Exchange ids, comma separated exchange list",
                Arity = ArgumentArity.ZeroOrMore,
                CustomParser = result => {
                    var listResult = new List<int>();

                    foreach (var token in result.Tokens) {
                        var rawValue = token.Value;

                        var rawStringArray = rawValue
                                             .Trim()
                                             .Split(new[] { ';', ',' }, StringSplitOptions.None);

                        if (!rawStringArray.Any()) {
                            result.AddError($"Invalid exchange id {token.Value}, cannot be empty");

                            return null!;
                        }

                        foreach (var rawString in rawStringArray) {
                            if (!int.TryParse(rawString.Trim(), out var value)) {
                                result.AddError($"Invalid exchange id in {token.Value}. Value {rawString}");

                                return null!;
                            }

                            listResult.Add(value);
                        }
                    }

                    return listResult;
                }
            };

            option.Aliases.Add("-i");

            return option;
        }

        private static Option<string> CreateFormatOption()
        {
            var defaultFormatValue = "{id} - {url} - {status}";

            var option = new Option<string>(
                "--format") {
                Description = "Specify how to format each matching exchanges to the outputted result." +
                             " The default value is \"{id} - {url} - {status}\"\r\n" +
                             $"Possible format values are : {string.Join(", ", FormatterRegistration.Indicators)}.\r\n" +
                             "Use this option to extract specific part of an exchange. Example: \"{response-body}\" for response body content, ...",
                Arity = ArgumentArity.ZeroOrOne,
                DefaultValueFactory = _ => defaultFormatValue
            };

            option.Aliases.Add("-f");

            return option;
        }

        private static Option<FileInfo?> CreateOutputFileOption()
        {
            var option = new Option<FileInfo?>(
                "--output-file") {
                Description = "Output the formatted result to a file instead of stdout",
                Arity = ArgumentArity.ZeroOrOne,
                CustomParser = result => {
                    if (!result.Tokens.Any())
                        return null;

                    return new FileInfo(result.Tokens.First().Value);
                }
            };

            option.Aliases.Add("-o");

            return option;
        }

        private static Option<bool> CreateUniqueOption()
        {
            var option = new Option<bool>(
                "--unique") {
                Description = "Result must be unique or exit error",
                Arity = ArgumentArity.ZeroOrOne,
                DefaultValueFactory = _ => false
            };

            option.Aliases.Add("-u");

            return option;
        }
    }
}
