using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Fluxzy.Core;

namespace Fluxzy.Cli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
            Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");

            //Environment.SetEnvironmentVariable("EnableH2Tracing", "true");
            //Environment.SetEnvironmentVariable("EnableH2TracingFilterHosts", "casalemedia.com");
            // Environment.SetEnvironmentVariable("EnableH1Tracing", "true");

            var exitCode =  await FluxzyStartup.Run(args, CancellationToken.None);

            return exitCode; 
        }
    }
}
