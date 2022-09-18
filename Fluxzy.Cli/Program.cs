using System;
using System.IO;
using Fluxzy.Core;

namespace Fluxzy.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            // Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
            Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");

            // Environment.SetEnvironmentVariable("EnableH2Tracing", "true");
            // Environment.SetEnvironmentVariable("EnableH1Tracing", "true");

            FluxzyCommand.Run(args);
            return 0;

            return new CliApp(s => new CertificateProvider(s, new FileSystemCertificateCache(s))).Start(args);
            
        }

        
    }
}
