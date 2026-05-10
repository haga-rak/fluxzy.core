// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands.Dissects;
using Fluxzy.Cli.Commands.Dissects.Formatters;
using Fluxzy.Readers;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public sealed class DissectCommand : AsyncCommand<DissectSettings>
    {
        private readonly IFluxzyConsole _console;

        public DissectCommand(IFluxzyConsole console)
        {
            _console = console;
        }

        protected override async Task<int> ExecuteAsync(CommandContext context, DissectSettings settings,
            CancellationToken cancellationToken)
        {
            var inputPath = settings.InputFileOrDirectory;

            IArchiveReader archiveReader;

            try {
                if (Directory.Exists(inputPath)) {
                    archiveReader = new DirectoryArchiveReader(inputPath);
                }
                else if (File.Exists(inputPath)) {
                    archiveReader = new FluxzyArchiveReader(inputPath);
                }
                else {
                    _console.Error.Write(
                        $"Input file or directory \"{inputPath}\" does not exists" + Environment.NewLine);

                    return 1;
                }
            }
            catch (Exception ex) {
                _console.Error.Write($"Cannot read {inputPath} : {ex.Message}" + Environment.NewLine);

                return 1;
            }

            HashSet<int>? exchangeIds = null;

            if (settings.Ids is { Length: > 0 }) {
                var collected = new List<int>();

                foreach (var token in settings.Ids) {
                    var rawArray = token.Trim().Split(new[] { ';', ',' }, StringSplitOptions.None);

                    if (rawArray.Length == 0) {
                        _console.Error.Write($"Invalid exchange id {token}, cannot be empty" + Environment.NewLine);

                        return 1;
                    }

                    foreach (var raw in rawArray) {
                        if (!int.TryParse(raw.Trim(), out var value)) {
                            _console.Error.Write(
                                $"Invalid exchange id in {token}. Value {raw}" + Environment.NewLine);

                            return 1;
                        }

                        collected.Add(value);
                    }
                }

                exchangeIds = collected.ToHashSet();
            }

            var dissectionOptions = new DissectionOptions(settings.Unique, exchangeIds, settings.Format);
            var flowManager = new DissectionFlowManager(new SequentialFormatter(),
                FormatterRegistration.Formatters);

            FileInfo? outputFile = null;

            if (!string.IsNullOrEmpty(settings.OutputFile)) {
                outputFile = new FileInfo(settings.OutputFile);
                outputFile.Directory?.Create();
            }

            Stream? outputFileStream = null;

            try {
                var stdout = outputFile != null
                    ? outputFileStream = outputFile.Create()
                    : _console.BinaryStdout;

                var stderr = _console.BinaryStderr;

                var result = await flowManager.Apply(archiveReader, stdout, stderr, dissectionOptions);

                return result ? 0 : 1;
            }
            finally {
                outputFileStream?.Dispose();
            }
        }
    }
}
