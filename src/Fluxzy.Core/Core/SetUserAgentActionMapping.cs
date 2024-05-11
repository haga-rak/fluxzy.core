// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Fluxzy.Core
{
    public class SetUserAgentActionMapping
    {
        public static SetUserAgentActionMapping Default { get; } = new SetUserAgentActionMapping(null);

        public SetUserAgentActionMapping(string ? configurationFile)
        {
            Map = string.IsNullOrWhiteSpace(configurationFile)
                ? new Dictionary<string, string>(
                    JsonSerializer.Deserialize<Dictionary<string, string>>(FileStore.UserAgents)!,
                    StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(
                    JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(configurationFile))!,
                    StringComparer.OrdinalIgnoreCase);
        }
        
        public Dictionary<string, string> Map { get; } 
    }
}
