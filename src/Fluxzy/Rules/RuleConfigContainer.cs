// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules
{
    /// <summary>
    /// This is a companion for deserialization rules from config files
    /// It allows multiple action declaration inside the same rule 
    /// </summary>
    public class RuleConfigContainer
    {
        [JsonConstructor]
        public RuleConfigContainer(Filter filter)
        {
            Filter = filter;
        }

        public string ? Name { get; set; }

        public Filter Filter { get; set; }

        public Action? Action { get; set; }

        public List<Action>? Actions { get; set; } = new(); 

        public int Order { get; set; }

        public IEnumerable<Action> GetAllActions()
        {
            if (Action != null) {
                yield return Action;
            }

            if (Actions != null) {
                foreach (var action in Actions) {
                    yield return action;
                }
            }
        }

        public IEnumerable<Rule> GetAllRules()
        {
            if (Action == null && (Actions == null || !Actions.Any())) {
                yield break;
            }

            if (Action != null) {
                yield return new Rule(Action, Filter); 
            }

            if (Actions != null) {
                foreach (var action in Actions) {
                    yield return new Rule(action, Filter);
                }
            }
        }


        public static IEnumerable<RuleConfigContainer> CreateFrom(IEnumerable<Rule> rules)
        {
            var groupping = rules.GroupBy(r => r.Filter);


            foreach (var group in groupping) {
                yield return new RuleConfigContainer(group.Key) {
                    Actions = group.Select(r => r.Action).ToList()
                }; 
            }
        }
    }
}
