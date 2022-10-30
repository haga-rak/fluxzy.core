using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Cli
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
            //Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");

            //Environment.SetEnvironmentVariable("EnableH2Tracing", "true");
            //Environment.SetEnvironmentVariable("EnableH2TracingFilterHosts", "casalemedia.com");
            // Environment.SetEnvironmentVariable("EnableH1Tracing", "true");

            var exitCode = await FluxzyStartup.Run(args, CancellationToken.None);

            return exitCode;
        }
    }
}
