// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Fluxzy.Clients.Ssl
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
            foreach (var (name, configuration) in ImpersonateProfileManager.GetBuiltInProfiles())
            {
                Instance.AddOrUpdateDefaultConfiguration(name, configuration);
            }
        }

        private readonly Dictionary<ImpersonateProfile, ImpersonateConfiguration> _configurations
            = new Dictionary<ImpersonateProfile, ImpersonateConfiguration>();

        private ImpersonateConfigurationManager()
        {

        }

        public ImpersonateConfiguration? LoadConfiguration(string nameOrConfigFile)
        {
            if (!ImpersonateProfile.TryParse(nameOrConfigFile, out var agent))
            {
                return null;
            }

            if (agent.Absolute)
            {
                if (_configurations.TryGetValue(agent, out var configuration))
                {
                    return configuration;
                }
            }

            if (agent.Latest)
            {
                var latest = _configurations.Keys
                                            .Where(a => string.Equals(a.Name, agent.Name,
                                                StringComparison.OrdinalIgnoreCase))
                                            .OrderByDescending(r => r.VersionAsVersion)
                                            .FirstOrDefault();

                if (latest != null)
                {
                    return _configurations[latest];
                }
            }

            if (File.Exists(nameOrConfigFile))
            {
                var json = File.ReadAllText(nameOrConfigFile);
                return JsonSerializer.Deserialize<ImpersonateConfiguration>(json, JsonSerializerOptions);
            }


            return null;
        }

        public void AddOrUpdateDefaultConfiguration(ImpersonateProfile profile, ImpersonateConfiguration configuration)
        {
            if (!profile.Absolute)
            {
                throw new ArgumentException("Agent must be a specific version", nameof(profile));
            }

            _configurations[profile] = configuration;
        }

        public IEnumerable<(ImpersonateProfile ConfigurationName, ImpersonateConfiguration)> GetConfigurations()
        {
            foreach (var (name, configuration) in _configurations)
            {
                yield return (name, configuration);
            }
        }
    }
}
