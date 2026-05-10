// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core.Pcap.Pcapng.Merge;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public sealed class DissectPcapCommand : AsyncCommand<DissectPcapSettings>
    {
        protected override Task<int> ExecuteAsync(CommandContext context, DissectPcapSettings settings,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings.OutputFile)) {
                throw new InvalidOperationException("--output-file (-o) is required");
            }

            var fileOrDirectory = settings.InputFileOrDirectory;

            if (File.Exists(fileOrDirectory)) {
                using var outStream = File.Create(settings.OutputFile);
                PcapMerge.MergeArchive(fileOrDirectory, outStream);

                return Task.FromResult(0);
            }

            if (Directory.Exists(fileOrDirectory)) {
                using var outStream = File.Create(settings.OutputFile);
                PcapMerge.MergeDumpDirectory(fileOrDirectory, outStream);

                return Task.FromResult(0);
            }

            throw new InvalidOperationException($"{fileOrDirectory} is neither a file or a directory");
        }
    }
}
