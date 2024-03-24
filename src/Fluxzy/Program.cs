// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Cli
{
    internal static class Program
    {
        internal static async Task<int> Main(string[] args)
        {
            if (ContainerEnvironmentHelper.IsInContainer(args)) {
                args = ContainerEnvironmentHelper.CreateArgsFromEnvironment(new SystemEnvironmentProvider());
            }

            if (Environment.GetEnvironmentVariable("appdata") == null) {
                Environment.SetEnvironmentVariable("appdata", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            }

            var exitCode = await FluxzyStartup.Run(args, null, CancellationToken.None);

            return exitCode;
        }
    }
}
