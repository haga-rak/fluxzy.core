// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Rules;

namespace Fluxzy.Core
{
    /// <summary>
    ///     Holds rules pre-partitioned by FilterScope for O(1) lookup in EnforceRules.
    ///     Immutable after construction; designed for atomic swap via volatile reference.
    /// </summary>
    internal sealed class PartitionedRules
    {
        private static readonly FilterScope[] PipelineScopes =
        {
            FilterScope.OnAuthorityReceived,
            FilterScope.RequestHeaderReceivedFromClient,
            FilterScope.DnsSolveDone,
            FilterScope.RequestBodyReceivedFromClient,
            FilterScope.ResponseHeaderReceivedFromRemote,
            FilterScope.ResponseBodyReceivedFromRemote
        };

        private readonly Dictionary<FilterScope, Rule[]> _byScope;

        public PartitionedRules(IReadOnlyList<Rule> allRules)
        {
            AllRules = allRules;
            _byScope = BuildPartitions(allRules);
        }

        /// <summary>
        ///     The complete ordered rule list. Used by GetCurrentAlterationRules().
        /// </summary>
        public IReadOnlyList<Rule> AllRules { get; }

        /// <summary>
        ///     Returns the rules that should execute for the given scope.
        /// </summary>
        public Rule[] GetRulesForScope(FilterScope scope)
        {
            return _byScope.TryGetValue(scope, out var rules) ? rules : Array.Empty<Rule>();
        }

        private static Dictionary<FilterScope, Rule[]> BuildPartitions(IReadOnlyList<Rule> allRules)
        {
            var buckets = new Dictionary<FilterScope, List<Rule>>();

            foreach (var scope in PipelineScopes) {
                buckets[scope] = new List<Rule>();
            }

            foreach (var rule in allRules) {
                var actionScope = rule.Action.ActionScope;

                if (actionScope == FilterScope.OutOfScope) {
                    // OutOfScope rules run at every pipeline scope
                    foreach (var scope in PipelineScopes) {
                        buckets[scope].Add(rule);
                    }
                }
                else if (actionScope == FilterScope.CopySibling) {
                    // CopySibling + MultipleScopeAction with non-null RunScope
                    if (rule.Action is MultipleScopeAction msa && msa.RunScope != null) {
                        if (buckets.TryGetValue(msa.RunScope.Value, out var bucket)) {
                            bucket.Add(rule);
                        }
                    }
                }
                else {
                    // Direct scope match
                    if (buckets.TryGetValue(actionScope, out var bucket)) {
                        bucket.Add(rule);
                    }
                }
            }

            var result = new Dictionary<FilterScope, Rule[]>(buckets.Count);

            foreach (var kvp in buckets) {
                result[kvp.Key] = kvp.Value.ToArray();
            }

            return result;
        }
    }
}
