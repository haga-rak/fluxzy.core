// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Tests.Cli.Scaffolding;
using Fluxzy.Tests.Common;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class CliRunOnceTest
    {
        [Fact]
        public async Task RunSingleTestPcap()
        {
            // Arrange 
            var protocol = "plainhttp11";
            var withPcap = true;
            var outputDirectory = true;
            var withSimpleRule = false;

            var rootDir = nameof(RunSingleTestPcap);

            var directoryName = $"{rootDir}/{protocol}-{withPcap}-{outputDirectory}-{withSimpleRule}";
            var fileName = $"{rootDir}/{protocol}-{withPcap}-{outputDirectory}-{withSimpleRule}.fxzy";

            var commandLine = "start -l 127.0.0.1/0";

            commandLine += outputDirectory ? $" -d {directoryName}" : $" -o {fileName}";
            commandLine += " -c";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);
            var requestBodyLength = 23632;
            var bodyLength = 0L;

            await using var fluxzyInstance = await commandLineHost.Run();

            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                $"{TestConstants.GetHost(protocol)}/global-health-check");

            requestMessage.Content = new StringContent(new string('z', 80), Encoding.UTF8, "text/plain");
            requestMessage.Headers.Add("X-Test-Header-256", "That value");

            // Act 
            using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

            bodyLength = response.Content.Headers.ContentLength ?? -1;

            var res = await response.Content.ReadAsStringAsync();


            //   await Task.Delay(2000);

            // using var response2 = await proxiedHttpClient.Client.SendAsync(requestMessage2);

            //Console.WriteLine("yo");
        }
    }
}
