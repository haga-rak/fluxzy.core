// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands.Dissects;
using Fluxzy.Cli.Commands.Dissects.Formatters;
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
            
            Argument<IArchiveReader> archiveReaderArgument = CreateInputFileOrDirectoryArgument(); 

            command.AddOption(CreateExchangeIdsOption());
            command.AddOption(CreateFormatOption());
            command.AddOption(CreateOutputFileOption());
            command.AddOption(CreateUniqueOption());
            command.AddArgument(archiveReaderArgument);

            command.SetHandler(context => Run(context, archiveReaderArgument));

            return command;
        }

        private async Task Run(InvocationContext context, Argument<IArchiveReader> archiveReaderArgument)
        {
            var exchangeIds = context.Value<List<int>?>("id");
            var format = context.Value<string>("format");
            var outputFile = context.Value<FileInfo?>("output-file");
            var mustBeUnique = context.Value<bool>("unique");

            var archiveReader =
                (IArchiveReader) context.ParseResult.GetValueForArgument(archiveReaderArgument);

            var dissectionOptions = new DissectionOptions(mustBeUnique, exchangeIds?.ToHashSet(), format);
            var flowManager = new DissectionFlowManager(new SequentialFormatter(),
                FormatterRegistration.Formatters);

            outputFile?.Directory?.Create();

            var stdout = outputFile != null ? outputFile.Create() : 
                (context.Console is OutputConsole outputConsole ?
                outputConsole.BinaryStdout : Console.OpenStandardOutput());

            var stdErr = context.Console is OutputConsole outputConsole2? 
                outputConsole2.BinaryStderr : Console.OpenStandardError();

            var result = await flowManager.Apply(archiveReader, stdout, stdErr, dissectionOptions);

            if (!result)
                context.ExitCode = 1;
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

                      return null! ;
                  })
            {
                Arity = ArgumentArity.ExactlyOne
            };

            return argument;
        }


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
