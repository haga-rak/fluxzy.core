// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public sealed class PackCommand : AsyncCommand<PackSettings>
    {
        protected override async Task<int> ExecuteAsync(CommandContext context, PackSettings settings,
            CancellationToken cancellationToken)
        {
            var directory = new DirectoryInfo(settings.InputDirectory);

            if (!directory.Exists) {
                throw new InvalidOperationException($"Directory does not exists {directory.FullName}");
            }

            var outputFile = new FileInfo(settings.OutputFile);
            outputFile.Directory?.Create();

            var packager = settings.Format == null
                ? PackagerRegistry.Instance.InferPackagerFromFileName(outputFile.Name)
                : PackagerRegistry.Instance.GetPackageOrDefault(settings.Format);

            await using var outputFileStream = outputFile.Create();
            await packager.Pack(directory.FullName, outputFileStream, null);

            return 0;
        }
    }
}
