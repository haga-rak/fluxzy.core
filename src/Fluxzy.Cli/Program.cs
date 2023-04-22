// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands;

namespace Fluxzy.Cli
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("appdata") == null) {
                // For Linux and OSX environment this EV is missing, so we need to set it manually 
                // to XDG_DATA_HOME

                Environment.SetEnvironmentVariable("appdata",
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            }

            Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");

            //Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");
            //Environment.SetEnvironmentVariable("EnableH2Tracing", "true");
            //Environment.SetEnvironmentVariable("EnableH2TracingFilterHosts", "casalemedia.com");
            // Environment.SetEnvironmentVariable("EnableH1Tracing", "true");

            var exitCode = await FluxzyStartup.Run(args, null, CancellationToken.None);

            return exitCode;
        }
    }
}
