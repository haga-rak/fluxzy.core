// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithServeH2Option
    {
        [Fact]
        public async Task ServeH2_ClientNegotiatesH2_ResponseVersionIs2()
        {
            var commandLine = "start -l 127.0.0.1:0 --no-cert-cache --serve-h2 --insecure";
            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var proxyInstance = await commandLineHost.Run();

            var proxyEndPoint = new IPEndPoint(IPAddress.Loopback, proxyInstance.ListenPort);

            using var client = Socks5ClientFactory.Create(proxyEndPoint, httpVersion: new Version(2, 0));

            var response = await client.GetAsync("https://sandbox.fluxzy.io:5001/ip");

            Assert.Equal(new Version(2, 0), response.Version);
            Assert.True(response.IsSuccessStatusCode,
                $"Expected success status code but got {response.StatusCode}");
        }
    }
}
