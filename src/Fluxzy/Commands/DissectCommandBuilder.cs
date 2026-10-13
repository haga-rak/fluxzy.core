// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands.Dissects;
using Fluxzy.Cli.Commands.Dissects.Formatters;
using Fluxzy.Cli.Commands.Dissects.Summary;
using Fluxzy.Cli.Commands.Dissects.Json;
using System.Text.Json;
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

            command.AddAlias("dis");
            
            var archiveReaderArgument = CreateInputFileOrDirectoryArgument(); 

            command.AddCommand(BuildExportPcapCommand());
            command.AddCommand(BuildSummaryCommand());

            command.AddOption(CreateExchangeIdsOption());
            command.AddOption(HostOption);
            command.AddOption(PathOption);
            command.AddOption(MethodOption);
            command.AddOption(StatusOption);
            command.AddOption(ContentTypeOption);
            command.AddOption(OffsetOption);
            command.AddOption(LimitOption);
            command.AddOption(CreateFormatOption());
            command.AddOption(CreateJsonOption());
            command.AddOption(CreateNdJsonOption());
            command.AddOption(CreateOutputFileOption());
            command.AddOption(CreateUniqueOption());
            command.AddArgument(archiveReaderArgument);

            command.SetHandler(context => Run(context, archiveReaderArgument));

            return command;
        }

        private static Command BuildExportPcapCommand()
        {
            var exportCommand = new Command("pcap", "Export pcapng files from an archive or dump directory.");

            var inputFileOrDirectory = new Argument<string>(
                "input-file-or-directory",
                description: "Input file or directory",
                parse: a => a.Tokens.First().Value)
            {
                Arity = ArgumentArity.ExactlyOne
            };
            
            var outFileOption = new Option<FileInfo>(
                "output-file",
                description: "Input file or directory",
                parseArgument: a => new FileInfo(a.Tokens.First().Value))
            {
                Arity = ArgumentArity.ExactlyOne,
                IsRequired = true,
            };

            outFileOption.AddAlias("-o");

            exportCommand.AddArgument(inputFileOrDirectory);
            exportCommand.AddOption(outFileOption);

            exportCommand.SetHandler((fileOrDirectory, outFile) => {

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
            }, inputFileOrDirectory, outFileOption);

            return exportCommand;
        }


        private static Command BuildSummaryCommand()
        {
            var summaryCommand = new Command("summary",
                "Print an overview of an archive: counts, hosts, status and method histograms, errors, time span.");

            var archiveReaderArgument = CreateInputFileOrDirectoryArgument();
            var jsonOption = CreateJsonOption();
            var outFileOption = CreateOutputFileOption();

            var maxHostsOption = new Option<int>(
                "--max-hosts",
                description: "Number of hosts to list, most requested first",
                getDefaultValue: () => 20);

            summaryCommand.AddArgument(archiveReaderArgument);
            summaryCommand.AddOption(jsonOption);
            summaryCommand.AddOption(outFileOption);
            summaryCommand.AddOption(maxHostsOption);

            summaryCommand.SetHandler(async context => {
                var archiveReader = context.ParseResult.GetValueForArgument(archiveReaderArgument);
                var json = context.ParseResult.GetValueForOption(jsonOption);
                var outputFile = context.ParseResult.GetValueForOption(outFileOption);
                var maxHosts = context.ParseResult.GetValueForOption(maxHostsOption);

                var source = context.ParseResult.GetValueForArgument(archiveReaderArgument) is DirectoryArchiveReader
                    ? "directory"
                    : "file";

                var sourceName = context.ParseResult.Tokens
                                        .Select(t => t.Value)
                                        .FirstOrDefault(v => File.Exists(v) || Directory.Exists(v)) ?? string.Empty;

                var summary = ArchiveSummaryBuilder.Build(archiveReader, sourceName, source, maxHosts);

                outputFile?.Directory?.Create();

                var stdout = outputFile != null ? outputFile.Create() :
                    context.Console is OutputConsole outputConsole ?
                        outputConsole.BinaryStdout : Console.OpenStandardOutput();

                await using var writer = new StreamWriter(stdout, new UTF8Encoding(false),
                    leaveOpen: outputFile == null);

                if (json) {
                    await writer.WriteAsync(JsonSerializer.Serialize(summary, DissectJsonOptions.Indented));
                    await writer.WriteLineAsync();
                }
                else {
                    ArchiveSummaryTextWriter.Write(summary, writer);
                }

                await writer.FlushAsync();
            });

            return summaryCommand;
        }

        private async Task Run(InvocationContext context, Argument<IArchiveReader> archiveReaderArgument)
        {
            var exchangeIds = context.Value<List<int>?>("id");
            var format = context.Value<string>("format");
            var outputFile = context.Value<FileInfo?>("output-file");
            var mustBeUnique = context.Value<bool>("unique");
            var json = context.Value<bool>("json");
            var ndJson = context.Value<bool>("ndjson");

            var archiveReader = context.ParseResult.GetValueForArgument(archiveReaderArgument);

            var outputMode = ndJson ? DissectOutputMode.NdJson
                : json ? DissectOutputMode.Json
                : DissectOutputMode.Format;

            var filter = new ExchangeFilterOptions {
                ExchangeIds = exchangeIds?.ToHashSet(),
                Host = context.ParseResult.GetValueForOption(HostOption),
                Path = context.ParseResult.GetValueForOption(PathOption),
                Method = context.ParseResult.GetValueForOption(MethodOption),
                Status = context.ParseResult.GetValueForOption(StatusOption),
                ContentType = context.ParseResult.GetValueForOption(ContentTypeOption),
                Offset = context.ParseResult.GetValueForOption(OffsetOption),
                Limit = context.ParseResult.GetValueForOption(LimitOption)
            };

            var dissectionOptions = new DissectionOptions(mustBeUnique, exchangeIds?.ToHashSet(), format,
                outputMode, filter);
            var flowManager = new DissectionFlowManager(new SequentialFormatter(),
                FormatterRegistration.Formatters);

            outputFile?.Directory?.Create();

            Stream? outputFileStream = null;

            try {
                var stdout = outputFile != null ? (outputFileStream = outputFile.Create()) :
                    (context.Console is OutputConsole outputConsole ?
                        outputConsole.BinaryStdout : Console.OpenStandardOutput());

                var stdErr = context.Console is OutputConsole outputConsole2 ?
                    outputConsole2.BinaryStderr : Console.OpenStandardError();

                var result = await flowManager.Apply(archiveReader, stdout, stdErr, dissectionOptions);

                if (!result)
                    context.ExitCode = 1;
            }
            finally {
                // ReSharper disable once MethodHasAsyncOverload
                outputFileStream?.Dispose();
            }
        }

        private static Argument<IArchiveReader> CreateInputFileOrDirectoryArgument()
        {
            var argument = new Argument<IArchiveReader>(
                  "input-file-or-directory",
                  description: "A fluxzy file or directory to dissect",
                  parse: t => {
                      var inputFileOrDirectory = t.Tokens.First().Value;

                      try {
                          if (Directory.Exists(inputFileOrDirectory)) {
                              return new DirectoryArchiveReader(inputFileOrDirectory);
                          }

                          if (File.Exists(inputFileOrDirectory)) {
                              return new FluxzyArchiveReader(inputFileOrDirectory);
                          }

                          t.ErrorMessage = $"Input file or directory \"{inputFileOrDirectory}\" does not exists";
                      }
                      catch (Exception ex) {
                          t.ErrorMessage = $"Cannot read {inputFileOrDirectory} : {ex.Message}";
                      }

                      return null!;
                  })
            {
                Arity = ArgumentArity.ExactlyOne
            };

            return argument;
        }


        private static readonly Option<string?> HostOption = new(
            "--host",
            "Keep exchanges whose host matches this glob (* and ?), case insensitive. Example: *.wikipedia.org");

        private static readonly Option<string?> PathOption = new(
            "--path",
            "Keep exchanges whose path (with query) matches this glob. Wrap in slashes for a regex: \"/^\\/api\\/.*/\"");

        private static readonly Option<string?> MethodOption = new(
            "--method",
            "Keep exchanges with one of these methods, comma separated. Example: GET,POST");

        private static readonly Option<string?> StatusOption = new(
            "--status",
            "Keep exchanges with one of these statuses: code (404), range (400-499) or class (5xx), comma separated");

        private static readonly Option<string?> ContentTypeOption = new(
            "--content-type",
            "Keep exchanges whose simplified content type contains this text. Example: json");

        private static readonly Option<int> OffsetOption = new(
            "--offset",
            () => 0,
            "Skip this many matching exchanges");

        private static readonly Option<int?> LimitOption = new(
            "--limit",
            "Output at most this many matching exchanges");

        private static Option<List<int>?> CreateExchangeIdsOption()
        {
            var option = new Option<List<int>?>(
                "--id",
                result => {
                    var listResult = new List<int>();

                    foreach (var token in result.Tokens) {
                        var rawValue = token.Value;

                        var rawStringArray = rawValue
                                             .Trim()
                                             .Split(new[] { ';', ',' }, StringSplitOptions.None);

                        if (!rawStringArray.Any()) {
                            result.ErrorMessage = $"Invalid exchange id {token.Value}, cannot be empty";

                            return null!;
                        }

                        foreach (var rawString in rawStringArray) {
                            if (!int.TryParse(rawString.Trim(), out var value)) {
                                result.ErrorMessage = $"Invalid exchange id in {token.Value}. Value {rawString}";

                                return null!;
                            }

                            listResult.Add(value);
                        }
                    }

                    return listResult;
                },
                description: "Exchange ids, comma separated exchange list"
            );

            option.AddAlias("-i");
            option.Arity = ArgumentArity.ZeroOrMore;
            option.SetDefaultValue(null);

            return option;
        }

        private static Option<string> CreateFormatOption()
        {
            var defaultFormatValue = "{id} - {url} - {status}";

            var option = new Option<string>(
                "--format",
                description: "Specify how to format each matching exchanges to the outputted result." +
                             " The default value is \"{id} - {url} - {status}\"\r\n" +
                             $"Possible format values are : {string.Join(", ", FormatterRegistration.Indicators)}.\r\n" +
                             "Use this option to extract specific part of an exchange. Example: \"{response-body}\" for response body content, ...",
                getDefaultValue: () => defaultFormatValue) {
                Arity = ArgumentArity.ZeroOrOne
            };

            option.AddAlias("-f");

            return option;
        }

        private static Option<FileInfo?> CreateOutputFileOption()
        {
            var option = new Option<FileInfo?>(
                "--output-file",
                parseArgument: result => {
                    if (!result.Tokens.Any())
                        return null;

                    return new FileInfo(result.Tokens.First().Value);
                },
                description: "Output the formatted result to a file instead of stdout") {
                Arity = ArgumentArity.ZeroOrOne
            };

            option.AddAlias("-o");
            option.SetDefaultValue(null);

            return option;
        }

        private static Option<bool> CreateJsonOption()
        {
            var option = new Option<bool>(
                "--json",
                description: "Output matching exchanges as a JSON array with a stable schema. Overrides --format.",
                getDefaultValue: () => false) {
                Arity = ArgumentArity.ZeroOrOne
            };

            option.AddAlias("-j");

            return option;
        }

        private static Option<bool> CreateNdJsonOption()
        {
            var option = new Option<bool>(
                "--ndjson",
                description: "Output one JSON object per line (newline delimited). Same schema as --json.",
                getDefaultValue: () => false) {
                Arity = ArgumentArity.ZeroOrOne
            };

            return option;
        }

        private static Option<bool> CreateUniqueOption()
        {
            var option = new Option<bool>(
                       "--unique",
                      description: "Result must be unique or exit error",
                      getDefaultValue: () => false)
            {
                Arity = ArgumentArity.ZeroOrOne
            };

            option.AddAlias("-u");

            return option;
        }
    }
}
