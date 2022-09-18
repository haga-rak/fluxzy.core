// Copyright © 2022 Haga Rakotoharivelo

using System.CommandLine;

namespace Fluxzy.Cli
{
    public class FluxzyCommand
    {
        public static void Run(string [] args)
        {
            RootCommand command = new RootCommand("Advanced HTTP capture tool");
            command.Add(StartCommandBuilder.Build());

            command.Invoke(args);
        }
    }
}