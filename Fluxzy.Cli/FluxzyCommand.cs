// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.CommandLine;

namespace Fluxzy.Cli
{
    public class FluxzyCommand
    {
        public static void Run(string [] args)
        {
            var command = new RootCommand("Advanced HTTP capture tool");
            var instanceIdentifier = Guid.NewGuid().ToString();

            var fluxzyStartCommand = new FluxzyStartCommand(instanceIdentifier);

            command.Add(fluxzyStartCommand.Build());

            command.Invoke(args);
        }
    }
}