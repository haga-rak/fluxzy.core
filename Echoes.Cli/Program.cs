using System;
using Echoes.Core;

namespace Echoes.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            // Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
            // Environment.SetEnvironmentVariable("InsertEchoesMetricsOnResponseHeader", "true");

            // Environment.SetEnvironmentVariable("EnableH2Tracing", "true");
            // Environment.SetEnvironmentVariable("EnableH1Tracing", "true");

            //Task.Run(async () =>
            //{
            //    await Task.Delay(3000);
            //    System.GC.Collect();
            //}); 

            return new CliApp(s => new CertificateProvider(s, new FileSystemCertificateCache(s))).Start(args);
       
        }
    }
}
