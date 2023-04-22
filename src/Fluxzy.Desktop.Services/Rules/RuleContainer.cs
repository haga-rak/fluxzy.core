// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using Fluxzy.Rules;

namespace Fluxzy.Desktop.Services.Rules
{
    public class RuleContainer
    {
        public RuleContainer(Rule rule)
        {
            Rule = rule;
        }

        [JsonConstructor]
        public RuleContainer(Rule rule, bool enabled)
        {
            Rule = rule;
            Enabled = enabled;
        }

        public Rule Rule { get; }

        public bool Enabled { get; }
    }
}
