using System;
using Echoes.Core;
using Echoes.H2.Encoder.HPack;

namespace Echoes.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            Environment.SetEnvironmentVariable("TracingDirectory", @"d:\debug-net");
            Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");

            Environment.SetEnvironmentVariable("EnableH2Tracing", "true");
            Environment.SetEnvironmentVariable("EnableH1Tracing", "true");

            return new CliApp(s => new CertificateProvider(s, new FileSystemCertificateCache(s))).Start(args);
       
        }
    }
}
