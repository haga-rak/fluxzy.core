// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using Fluxzy.Core;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class PartitionedRulesTests
    {
        [Fact]
        public void DirectScopeMatch_RuleLandsInCorrectBucket()
        {
            var rule = new Rule(
                new AddRequestHeaderAction("X-Test", "v"), AnyFilter.Default);

            var partitioned = new PartitionedRules(new[] { rule });

            Assert.Contains(rule,
                partitioned.GetRulesForScope(FilterScope.RequestHeaderReceivedFromClient));
            Assert.DoesNotContain(rule,
                partitioned.GetRulesForScope(FilterScope.OnAuthorityReceived));
            Assert.DoesNotContain(rule,
                partitioned.GetRulesForScope(FilterScope.ResponseHeaderReceivedFromRemote));
        }

        [Fact]
        public void OutOfScope_RuleAppearsInAllPipelineBuckets()
        {
            var rule = new Rule(new StdOutAction("test"), AnyFilter.Default);

            var partitioned = new PartitionedRules(new[] { rule });

            Assert.Contains(rule, partitioned.GetRulesForScope(FilterScope.OnAuthorityReceived));
            Assert.Contains(rule, partitioned.GetRulesForScope(FilterScope.RequestHeaderReceivedFromClient));
            Assert.Contains(rule, partitioned.GetRulesForScope(FilterScope.DnsSolveDone));
            Assert.Contains(rule, partitioned.GetRulesForScope(FilterScope.RequestBodyReceivedFromClient));
            Assert.Contains(rule, partitioned.GetRulesForScope(FilterScope.ResponseHeaderReceivedFromRemote));
            Assert.Contains(rule, partitioned.GetRulesForScope(FilterScope.ResponseBodyReceivedFromRemote));
        }

        [Fact]
        public void MultipleScopeAction_WithRunScope_LandsInRunScopeBucket()
        {
            var action = new StdErrAction("test") {
                RunScope = FilterScope.ResponseHeaderReceivedFromRemote
            };
            var rule = new Rule(action, AnyFilter.Default);

            var partitioned = new PartitionedRules(new[] { rule });

            Assert.Contains(rule,
                partitioned.GetRulesForScope(FilterScope.ResponseHeaderReceivedFromRemote));
            Assert.DoesNotContain(rule,
                partitioned.GetRulesForScope(FilterScope.OnAuthorityReceived));
            Assert.DoesNotContain(rule,
                partitioned.GetRulesForScope(FilterScope.RequestHeaderReceivedFromClient));
        }

        [Fact]
        public void MultipleScopeAction_NullRunScope_LandsInNoBucket()
        {
            var action = new StdErrAction("test") { RunScope = null };
            var rule = new Rule(action, AnyFilter.Default);

            var partitioned = new PartitionedRules(new[] { rule });

            Assert.Empty(partitioned.GetRulesForScope(FilterScope.OnAuthorityReceived));
            Assert.Empty(partitioned.GetRulesForScope(FilterScope.RequestHeaderReceivedFromClient));
            Assert.Empty(partitioned.GetRulesForScope(FilterScope.DnsSolveDone));
            Assert.Empty(partitioned.GetRulesForScope(FilterScope.RequestBodyReceivedFromClient));
            Assert.Empty(partitioned.GetRulesForScope(FilterScope.ResponseHeaderReceivedFromRemote));
            Assert.Empty(partitioned.GetRulesForScope(FilterScope.ResponseBodyReceivedFromRemote));
        }

        [Fact]
        public void OrderPreserved_WithinScopeBucket()
        {
            var rule1 = new Rule(
                new AddRequestHeaderAction("X-First", "1"), AnyFilter.Default);
            var rule2 = new Rule(
                new AddRequestHeaderAction("X-Second", "2"), AnyFilter.Default);
            var rule3 = new Rule(
                new AddRequestHeaderAction("X-Third", "3"), AnyFilter.Default);

            var partitioned = new PartitionedRules(new[] { rule1, rule2, rule3 });

            var bucket = partitioned.GetRulesForScope(
                FilterScope.RequestHeaderReceivedFromClient);

            Assert.Equal(3, bucket.Length);
            Assert.Same(rule1, bucket[0]);
            Assert.Same(rule2, bucket[1]);
            Assert.Same(rule3, bucket[2]);
        }

        [Fact]
        public void AllRules_ContainsCompleteList()
        {
            var ruleA = new Rule(
                new SkipSslTunnelingAction(), AnyFilter.Default);
            var ruleB = new Rule(
                new AddResponseHeaderAction("X-Test", "v"), AnyFilter.Default);

            var partitioned = new PartitionedRules(new[] { ruleA, ruleB });

            Assert.Equal(2, partitioned.AllRules.Count);
            Assert.Same(ruleA, partitioned.AllRules[0]);
            Assert.Same(ruleB, partitioned.AllRules[1]);
        }

        [Fact]
        public void GetRulesForScope_UnknownScope_ReturnsEmpty()
        {
            var rule = new Rule(
                new AddRequestHeaderAction("X-Test", "v"), AnyFilter.Default);

            var partitioned = new PartitionedRules(new[] { rule });

            Assert.Empty(partitioned.GetRulesForScope(FilterScope.CopySibling));
            Assert.Empty(partitioned.GetRulesForScope(FilterScope.OutOfScope));
        }

        [Fact]
        public void MixedRules_CorrectPartitioning()
        {
            var authorityRule = new Rule(
                new SkipSslTunnelingAction(), AnyFilter.Default);
            var requestRule = new Rule(
                new AddRequestHeaderAction("X-Req", "v"), AnyFilter.Default);
            var responseRule = new Rule(
                new AddResponseHeaderAction("X-Res", "v"), AnyFilter.Default);
            var outOfScopeRule = new Rule(
                new StdOutAction("log"), AnyFilter.Default);
            var multiScopeRule = new Rule(
                new StdErrAction("err") { RunScope = FilterScope.DnsSolveDone },
                AnyFilter.Default);

            var allRules = new[] {
                authorityRule, requestRule, responseRule, outOfScopeRule, multiScopeRule
            };

            var partitioned = new PartitionedRules(allRules);

            // OnAuthorityReceived: authorityRule + outOfScopeRule
            var authority = partitioned.GetRulesForScope(FilterScope.OnAuthorityReceived);
            Assert.Equal(2, authority.Length);
            Assert.Same(authorityRule, authority[0]);
            Assert.Same(outOfScopeRule, authority[1]);

            // RequestHeaderReceivedFromClient: requestRule + outOfScopeRule
            var request = partitioned.GetRulesForScope(FilterScope.RequestHeaderReceivedFromClient);
            Assert.Equal(2, request.Length);
            Assert.Same(requestRule, request[0]);
            Assert.Same(outOfScopeRule, request[1]);

            // DnsSolveDone: outOfScopeRule + multiScopeRule
            var dns = partitioned.GetRulesForScope(FilterScope.DnsSolveDone);
            Assert.Equal(2, dns.Length);
            Assert.Same(outOfScopeRule, dns[0]);
            Assert.Same(multiScopeRule, dns[1]);

            // ResponseHeaderReceivedFromRemote: responseRule + outOfScopeRule
            var response = partitioned.GetRulesForScope(FilterScope.ResponseHeaderReceivedFromRemote);
            Assert.Equal(2, response.Length);
            Assert.Same(responseRule, response[0]);
            Assert.Same(outOfScopeRule, response[1]);
        }

        [Fact]
        public void EmptyRuleList_AllBucketsEmpty()
        {
            var partitioned = new PartitionedRules(System.Array.Empty<Rule>());

            Assert.Empty(partitioned.GetRulesForScope(FilterScope.OnAuthorityReceived));
            Assert.Empty(partitioned.GetRulesForScope(FilterScope.RequestHeaderReceivedFromClient));
            Assert.Empty(partitioned.GetRulesForScope(FilterScope.DnsSolveDone));
            Assert.Empty(partitioned.GetRulesForScope(FilterScope.ResponseHeaderReceivedFromRemote));
            Assert.Empty(partitioned.AllRules);
        }
    }
}
