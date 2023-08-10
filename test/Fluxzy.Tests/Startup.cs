// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using Fluxzy.Certificates;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Fluxzy.Tests.Startup", "Fluxzy.Tests")]

namespace Fluxzy.Tests
{
    public class Startup : XunitTestFramework
    {
        public Startup(IMessageSink messageSink)
            : base(messageSink)
        {
            InstallCertificate(); 


            //Environment.SetEnvironmentVariable("Fluxzy_EnableNetworkFileDump", "true");
            //Environment.SetEnvironmentVariable("Fluxzy_EnableWindowSizeTrace", "true");

            //Environment.SetEnvironmentVariable("EnableH1Tracing", "true");
            //Environment.SetEnvironmentVariable("EnableH2Tracing", "true");

            //Environment.SetEnvironmentVariable("EnableH2TracingFilterHosts",
            //    "2befficient.fr;smartizy.com; discord.com; facebook.com; google.com");
        }

        private void InstallCertificate()
        {
            DefaultCertificateAuthorityManager authorityManager = new();
            authorityManager.CheckAndInstallCertificate(FluxzySetting.CreateDefault(IPAddress.Loopback, 4444).CaCertificate.GetX509Certificate());
        }

        public new void Dispose()
        {
            // Place tear down code here
            base.Dispose();
        }
    }
}
