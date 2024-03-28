// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands;

namespace Fluxzy.Cli
{
    public static class FluxzyStartup
    {
        public static async Task<int> Run(string[] args, OutputConsole? outputConsole, CancellationToken token)
        {
            if (Environment.GetEnvironmentVariable("FLUXZY_STDOUT_ARGS") == "1") {
                Console.WriteLine(string.Join(" ", args));
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
            rootCommand.Add(certificateCommandBuilder.Build());
            rootCommand.Add(packCommandBuilder.Build());
            rootCommand.Add(dissectCommandBuilder.Build());

            var final = new CommandLineBuilder(rootCommand)
                        .UseVersionOption("-v")
                        .UseHelp()
                        .UseEnvironmentVariableDirective()
                        .UseParseDirective()
                        .UseTypoCorrections()
                        .UseParseErrorReporting()
                        .CancelOnProcessTermination()
                        .UseExceptionHandler((e, context) => {
                            Console.ForegroundColor = ConsoleColor.Red;
                            context.Console.Error.WriteLine(e.Message);
                            Console.ResetColor();
                            context.ExitCode = 1;
                        }, 1)
                        .Build();

            try {
                var exitCode = outputConsole == null
                    ? await final.InvokeAsync(args)
                    : await final.InvokeAsync(args, outputConsole);

                return exitCode;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);

                return 1;
            }
        }
    }
}
