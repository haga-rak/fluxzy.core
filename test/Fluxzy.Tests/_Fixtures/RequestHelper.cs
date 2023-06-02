// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests._Fixtures
{
    public static class RequestHelper
    {
        public static async Task<(ExchangeInfo, ConnectionInfo, DirectoryArchiveReader)>
            DirectRequest(HttpRequestMessage requestMessage, string? ruleContent = null)
        {
            var directory = "test-artifacts/" + nameof(DirectRequest) + "/" + Guid.NewGuid();
            var commandLine = $"start -l 127.0.0.1/0 -d {directory}";

            if (ruleContent != null)
                commandLine += " -R";
            
            await using (var fluxzyInstance = await FluxzyCommandLineHost.CreateAndRun(commandLine, ruleContent)) {
                using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);
                var response = await proxiedHttpClient.Client.SendAsync(requestMessage);
                await response.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            using var archiveReader = new DirectoryArchiveReader(directory);

            var exchanges = archiveReader.ReadAllExchanges().ToList();
            var connections = archiveReader.ReadAllConnections().ToList();

            return (exchanges.FirstOrDefault()!, connections.FirstOrDefault()!, archiveReader);
        }

        public static async Task<ProxyInstance>
            WaitForSingleRequest(string? ruleContent = null)
        {
            var directory = "test-artifacts/" + nameof(DirectRequest) + "/" + Guid.NewGuid();
            var commandLine = $"start -l 127.0.0.1/0 -d {directory} -n 1";

            if (ruleContent != null)
                commandLine += " -R";

            return await FluxzyCommandLineHost.CreateAndRun(commandLine, ruleContent);
        }
    }
}
