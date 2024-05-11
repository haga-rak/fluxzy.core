// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Fluxzy.Core
{
    public class UserAgentActionMapping
    {
        public static UserAgentActionMapping Default { get; } = new UserAgentActionMapping(null);

        public UserAgentActionMapping(string ? configurationFile)
        {
            Map = string.IsNullOrWhiteSpace(configurationFile)
                ? new Dictionary<string, string>(
                    JsonSerializer.Deserialize<Dictionary<string, string>>(FileStore.UserAgents)!,
                    StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(
                    JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(configurationFile))!,
                    StringComparer.OrdinalIgnoreCase);
        }
        
        public Dictionary<string, string> Map { get; set; } = new();
    }
}
