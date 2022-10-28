﻿// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Cli
{
    public class FluxzyStartup
    {
        public static async Task<int> Run(string [] args, CancellationToken token)
        {
            var rootCommand = new RootCommand("Advanced HTTP capture tool");
            var instanceIdentifier = Guid.NewGuid().ToString();

            var startCommandBuilder = new StartCommandBuilder(instanceIdentifier);
            var certificateCommandBuilder = new CertificateCommandBuilder(); 
            var packCommandBuilder = new PackCommandBuilder(); 

            rootCommand.Add(startCommandBuilder.Build(token));
            rootCommand.Add(certificateCommandBuilder.Build());
            rootCommand.Add(packCommandBuilder.Build());

            var final = new CommandLineBuilder(rootCommand)
                        .UseVersionOption()
                        .UseHelp()
                        .UseEnvironmentVariableDirective()
                        .UseParseDirective()
                        //.UseSuggestDirective()
                        //.RegisterWithDotnetSuggest()
                        .UseTypoCorrections()
                        .UseParseErrorReporting()
                        .CancelOnProcessTermination()
                        .UseExceptionHandler((e, context) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red; 
                        context.Console.Error.WriteLine(e.Message);
                        Console.ResetColor();
                        context.ExitCode = 1;
                    }, 1)
                    .Build();
            
            try
            {
                var exitCode =  await final.InvokeAsync(args);
                return exitCode; 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }
    }
}