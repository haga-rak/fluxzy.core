// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests.Cli.Scaffolding;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionBase : IAsyncDisposable
    {
        private ProxyInstance? _fluxzyInstance;
        private ProxiedHttpClient? _proxiedHttpClient;
        private string? _ruleFile;

        protected async Task<HttpResponseMessage> Exec(string yamlContent, HttpRequestMessage requestMessage)
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1:0";
            var uniqueIdentifier = Guid.NewGuid().ToString();

            _ruleFile = $"{uniqueIdentifier}.yml";
            
            await File.WriteAllTextAsync(_ruleFile, yamlContent);

            commandLine += $" -r {_ruleFile}";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            _fluxzyInstance = await commandLineHost.Run();

            _proxiedHttpClient = new ProxiedHttpClient(_fluxzyInstance.ListenPort);

            return  await _proxiedHttpClient.Client.SendAsync(requestMessage);
        }

        public async ValueTask DisposeAsync()
        { 
            if (_ruleFile != null && File.Exists(_ruleFile))
                File.Delete(_ruleFile);


            _proxiedHttpClient?.Dispose();

            if (_fluxzyInstance != null)
                await _fluxzyInstance.DisposeAsync();
        }
    }
}
