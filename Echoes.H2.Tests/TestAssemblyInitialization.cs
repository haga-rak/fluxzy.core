// Copyright © 2022 Haga Rakotoharivelo

using System;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("Echoes.H2.Tests.TestAssemblyInitialization", "Echoes.Tests")]

namespace Echoes.H2.Tests
{
    public class TestAssemblyInitialization : XunitTestFramework
    {
        public TestAssemblyInitialization(IMessageSink messageSink)
            : base(messageSink)
        {

            //Environment.SetEnvironmentVariable("Echoes_EnableNetworkFileDump", "true");
            //Environment.SetEnvironmentVariable("Echoes_EnableWindowSizeTrace", "true");
        }

        public new void Dispose()
        {
            // Place tear down code here
            base.Dispose();
        }
    }
}