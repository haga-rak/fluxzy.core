// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Rules
{
    public class RuleSet
    {
        public RuleSet(params Rule[] rules)
        {
            Rules = rules.GroupBy(g => g.Filter.Identifier)
                         .Select(s => new RuleConfigContainer(s.First().Filter) {
                             Actions = s.Select(sm => sm.Action).ToList()
                         }).ToList();
        }

        public List<RuleConfigContainer> Rules { get; set; }
    }
}
