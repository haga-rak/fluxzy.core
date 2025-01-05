// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Fluxzy.Rules.Actions
{
    public class ImpersonateConfigurationManager
    {
        public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping

        };

        public static ImpersonateConfigurationManager Instance { get; } = new ImpersonateConfigurationManager();

        static ImpersonateConfigurationManager()
        {
            foreach (var (name, configuration) in PredefinedImpersonateConfigurationLoader.GetPredefined())
            {
                Instance.AddOrUpdateDefaultConfiguration(name, configuration);
            }
        }

        private readonly Dictionary<ImpersonateAgent, ImpersonateConfiguration> _configurations 
            = new Dictionary<ImpersonateAgent, ImpersonateConfiguration>();

        private ImpersonateConfigurationManager()
        {

        }

        public ImpersonateConfiguration? LoadConfiguration(string nameOrConfigFile)
        {
            if (File.Exists(nameOrConfigFile)) {
                var json = File.ReadAllText(nameOrConfigFile);
                return JsonSerializer.Deserialize<ImpersonateConfiguration>(json, JsonSerializerOptions);
            }

            if (!ImpersonateAgent.TryParse(nameOrConfigFile, out var agent))
            {
                return null;
            }

            if (agent.Absolute) {
                if (_configurations.TryGetValue(agent, out var configuration))
                {
                    return configuration;
                }
            }

            if (agent.Latest) {
                var latest = _configurations.Keys
                    .Where(a => string.Equals(a.Name, agent.Name, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => a.VersionAsVersion)
                    .FirstOrDefault();

                if (latest != null)
                {
                    return _configurations[latest];
                }
            }

            return null;
        }

        public void AddOrUpdateDefaultConfiguration(ImpersonateAgent agent, ImpersonateConfiguration configuration)
        {
            if (!agent.Absolute)
            {
                throw new ArgumentException("Agent must be a specific version", nameof(agent));
            }

            _configurations[agent] = configuration;
        }

        public IEnumerable<(ImpersonateAgent ConfigurationName, ImpersonateConfiguration)> GetConfigurations()
        {
            foreach (var (name, configuration) in _configurations)
            {
                yield return (name, configuration);
            }
        }
    }
}
