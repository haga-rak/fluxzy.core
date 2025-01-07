// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Text.Json;
using Fluxzy.Clients.Ssl;
using Xunit;

namespace Fluxzy.Tests
{
    public class ImpersonateConfigurationTests
    {
        [Fact]
        public void OutputDefault()
        {
            var allConfigurations = ImpersonateConfigurationManager.Instance.GetConfigurations();

            Directory.CreateDirectory("Impersonate");

            foreach (var (name, configuration) in allConfigurations)
            {
                var json = JsonSerializer.Serialize(configuration, 
                    ImpersonateConfigurationManager.JsonSerializerOptions);

                File.WriteAllText($"Impersonate/{name}.json", json);
            }
        }
    }
}
