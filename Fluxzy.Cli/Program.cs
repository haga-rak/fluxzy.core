using System;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Core;

namespace Fluxzy.Cli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
            Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");

            // Environment.SetEnvironmentVariable("EnableH2Tracing", "true");
            // Environment.SetEnvironmentVariable("EnableH1Tracing", "true");

            return await FluxzyCommand.Run(args);

            return new CliApp(s => new CertificateProvider(s, new FileSystemCertificateCache(s))).Start(args);
            
        }

        
    }
}
