using System;
using Echoes.Core;

namespace Echoes.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            Environment.SetEnvironmentVariable("Echoes_EnableNetworkFileDump", "true");
            return new CliApp(s => new CertificateProvider(s, new FileSystemCertificateCache(s))).Start(args);
        }
    }
}
