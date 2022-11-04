// Copyright © 2022 Haga Rakotoharivelo

using System;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("Fluxzy.H2.Tests.TestAssemblyInitialization", "Fluxzy.Tests")]

namespace Fluxzy.Tests
{
    public class TestAssemblyInitialization : XunitTestFramework
    {
        public TestAssemblyInitialization(IMessageSink messageSink)
            : base(messageSink)
        {
            //Environment.SetEnvironmentVariable("Fluxzy_EnableNetworkFileDump", "true");
            //Environment.SetEnvironmentVariable("Fluxzy_EnableWindowSizeTrace", "true");

            //Environment.SetEnvironmentVariable("EnableH1Tracing", "true");
            //Environment.SetEnvironmentVariable("EnableH2Tracing", "true");

            //Environment.SetEnvironmentVariable("EnableH2TracingFilterHosts",
            //    "2befficient.fr;smartizy.com; discord.com; facebook.com; google.com");
        }

        public new void Dispose()
        {
            // Place tear down code here
            base.Dispose();
        }
    }
}