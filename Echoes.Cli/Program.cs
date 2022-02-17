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
            Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
            return new CliApp(s => new CertificateProvider(s, new FileSystemCertificateCache(s))).Start(args);
        }
    }
}
