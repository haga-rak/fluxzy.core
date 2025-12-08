// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-r

using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands;
using Fluxzy.Core;

namespace Fluxzy.Cli
{
    public static class FluxzyStartup
    {
        public static async Task<int> Run(string[] args, OutputConsole? outputConsole, CancellationToken token, 
            EnvironmentProvider? environmentProvider = null)
        {
            var currentEnvironmentProvider = environmentProvider ?? new SystemEnvironmentProvider();

            if (currentEnvironmentProvider.GetEnvironmentVariable("FLUXZY_STDOUT_ARGS") == "1") {
                var outWriter = outputConsole?.Out ?? Console.Out;
                outWriter.WriteLine(string.Join(" ", args));
            }

            var rootCommand =
                new RootCommand(
                    "CLI tool for recording, analyzing and altering HTTP/1.1, H2, WebSocket traffic over plain or secure channels. Visit https://fluxzy.io for more info.");

            var instanceIdentifier = Guid.NewGuid().ToString();

            var startCommandBuilder = new StartCommandBuilder(instanceIdentifier);
            var certificateCommandBuilder = new CertificateCommandBuilder();
            var packCommandBuilder = new PackCommandBuilder();
            var dissectCommandBuilder = new DissectCommandBuilder();

            rootCommand.Add(startCommandBuilder.Build(token));
            rootCommand.Add(certificateCommandBuilder.Build(currentEnvironmentProvider));
            rootCommand.Add(packCommandBuilder.Build());
            rootCommand.Add(dissectCommandBuilder.Build());

            try {
                var parseResult = rootCommand.Parse(args);

                var configuration = new InvocationConfiguration();

                if (outputConsole != null) {
                    configuration.Output = outputConsole.Out;
                    configuration.Error = outputConsole.Error;
                }

                var exitCode = await parseResult.InvokeAsync(configuration, token);

                return exitCode;
            }
            catch (Exception ex) {
                var errorWriter = outputConsole?.Error ?? Console.Error;
                errorWriter.WriteLine(ex.Message);

                return 1;
            }
        }
    }
}
