// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata("Change the User-Agent" +
                    "This action is used to change the User-Agent header of the request from a list of built-in user-agent values." +
                    "")]
    public class SetUserAgentAction : Action
    {
        public SetUserAgentAction(string name)
        {
            Name = name;
        }

        [ActionDistinctive]
        public string Name { get; set; }

        public override FilterScope ActionScope { get; } = FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription =>
            string.IsNullOrWhiteSpace(Name) ? "Change the User-Agent" : $"Change the User-Agent to {Name.Trim()}";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (string.IsNullOrWhiteSpace(Name)) {
                return default;
            }

            if (!context.SetUserAgentActionMapping.Map.TryGetValue(Name, out var userAgentValue)) {
                return default; // Not found in map 
            }

            context.RequestHeaderAlterations.Add(new HeaderAlterationReplace("user-agent", userAgentValue, true));

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            var defaultMapping = SetUserAgentActionMapping.Default;

            foreach (var (name, userAgentValue) in defaultMapping.Map.OrderBy(r => r.Key)) {
                yield return new ActionExample($"Change `User-Agent` to `{name}` (`{userAgentValue}`)",
                    new SetUserAgentAction(name));
            }
        }
    }
}
