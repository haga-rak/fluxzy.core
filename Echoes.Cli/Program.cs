using System;
using Echoes.Core;
using Echoes.H2.Encoder.HPack;

namespace Echoes.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            //throw new EchoesException("");
            //Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
            Environment.SetEnvironmentVariable("EnableH2Tracing", "true");
            Environment.SetEnvironmentVariable("EnableH2TracingFilterHosts", 
                "2befficient.fr;smartizy.com; discord.com; facebook.com; google.com");

            return new CliApp(s => new CertificateProvider(s, new FileSystemCertificateCache(s))).Start(args);
        }
    }
}
