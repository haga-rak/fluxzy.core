// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Fluxzy.Certificates;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Fluxzy.Tests._Fixtures.Configurations;
using Xunit;
using Action = Fluxzy.Rules.Action;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class RuleParsing
    {
        [Fact]
        public void Reading_Should_Parse_Basic_Rule()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: AnyFilter        
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var _)!;

            var targetAction = (rule.GetSingleAction() as AddRequestHeaderAction)!;

            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.NotNull(rule.GetSingleAction());
            Assert.Equal(AnyFilter.Default.Identifier, rule.Filter.Identifier);
            Assert.Equal(typeof(AddRequestHeaderAction), rule.GetSingleAction().GetType());
            Assert.Equal("fluxzy", targetAction.HeaderName);
            Assert.Equal("on", targetAction.HeaderValue);
        }

        [Fact]
        public void Reading_Should_Fail_Resolve_Filter_Invalid_Type_Kind()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: NoMoreFilter        
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var errorMessages)!;

            Assert.Null(rule);
            Assert.NotEmpty(errorMessages!);
            Assert.Contains(errorMessages!, r => r.Message.Contains("NoMoreFilter"));
        }

        [Fact]
        public void Reading_Should_Fail_InvalidYaml()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                fi lter 
                    typeKi'nd: NoMoreFilter        
                a ct'ion '
                  ty'pe Kind: AddRequestHeaderAction
                  he'ad erNa'me: fluxzy
                  he'ad erValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var errorMessages)!;

            Assert.Null(rule);
            Assert.NotEmpty(errorMessages!);
        }

        [Fact]
        public void Reading_Should_Fail_InvalidYaml_Indent()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: AnyFilter        
                action : 
                  typeKind: AddRequestHeaderAction
                   headerName: fluxzy 
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var errorMessages)!;

            Assert.Null(rule);
            Assert.NotEmpty(errorMessages!);
        }

        [Fact]
        public void Reading_Should_Fail_Invalid_Type()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: AnyFilter   
                  inverted: gogo
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: 52
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var errorMessages)!;

            Assert.Null(rule);
            Assert.NotEmpty(errorMessages!);
        }

        [Fact]
        public void Reading_Should_Fail_InvalidYaml_EmptyFile()
        {
            var ruleConfigReader = new RuleConfigParser();
            var yamlContent = "";

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var errorMessages)!;

            Assert.Null(rule);
            Assert.NotEmpty(errorMessages!);
        }

        [Fact]
        public void Reading_Should_Fail_No_Action()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: AnyFilter       
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var errorMessages)!;

            Assert.Null(rule);
            Assert.NotEmpty(errorMessages!);
        }

        [Fact]
        public void Reading_Should_Fail_Resolve_Filter_No_Type_Kind()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter:       
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var errorMessages)!;

            Assert.Null(rule);
            Assert.NotEmpty(errorMessages!);
        }

        [Fact]
        public void Reading_Should_Parse_List_Of_Int()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: StatusCodeFilter        
                  statusCodes:
                    - 200
                    - 204
                    - 301
                    - 302
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var _)!;

            var filter = (rule.Filter as StatusCodeFilter)!;

            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.NotNull(rule.GetSingleAction());
            Assert.Equal(typeof(AddRequestHeaderAction), rule.GetSingleAction().GetType());
            Assert.Equal(typeof(StatusCodeFilter), filter.GetType());
            Assert.Equal(4, filter.StatusCodes.Count);
            Assert.Contains(filter.StatusCodes, c => c == 200);
            Assert.Contains(filter.StatusCodes, c => c == 204);
            Assert.Contains(filter.StatusCodes, c => c == 301);
            Assert.Contains(filter.StatusCodes, c => c == 302);
        }

        [Fact]
        public void Reading_Testing_Camel_Case()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: statusCodeFilter        
                  statusCodes:
                    - 200
                    - 204
                    - 301
                    - 302
                action : 
                  typeKind: addRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var _)!;

            var filter = (rule.Filter as StatusCodeFilter)!;

            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.NotNull(rule.GetSingleAction());
            Assert.Equal(typeof(AddRequestHeaderAction), rule.GetSingleAction().GetType());
            Assert.Equal(typeof(StatusCodeFilter), filter.GetType());
            Assert.Equal(4, filter.StatusCodes.Count);
            Assert.Contains(filter.StatusCodes, c => c == 200);
            Assert.Contains(filter.StatusCodes, c => c == 204);
            Assert.Contains(filter.StatusCodes, c => c == 301);
            Assert.Contains(filter.StatusCodes, c => c == 302);
        }

        [Fact]
        public void Reading_Should_Parse_List_Of_Int_Mutli_Definition()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: StatusCodeFilter        
                  statusCodes:
                    - 200
                    - 204
                    - 301
                    - 302
                actions : 
                  - typeKind: AddRequestHeaderAction
                    headerName: fluxzy
                    headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var _)!;

            var filter = (rule.Filter as StatusCodeFilter)!;

            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.NotNull(rule.GetSingleAction());
            Assert.Equal(typeof(AddRequestHeaderAction), rule.GetSingleAction().GetType());
            Assert.Equal(typeof(StatusCodeFilter), filter.GetType());
            Assert.Equal(4, filter.StatusCodes.Count);
            Assert.Contains(filter.StatusCodes, c => c == 200);
            Assert.Contains(filter.StatusCodes, c => c == 204);
            Assert.Contains(filter.StatusCodes, c => c == 301);
            Assert.Contains(filter.StatusCodes, c => c == 302);
        }

        [Fact]
        public void Reading_Should_Parse_Multiple_Actions_Mutli_Definition()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: StatusCodeFilter        
                  statusCodes:
                    - 200
                    - 204
                    - 301
                    - 302
                actions : 
                  - typeKind: AddRequestHeaderAction
                    headerName: fluxzy
                    headerValue: on
                  - typeKind: DeleteRequestHeaderAction
                    headerName: x-server
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var _)!;

            var filter = (rule.Filter as StatusCodeFilter)!;

            var actions = rule.GetAllActions().ToList();

            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.Equal(2, actions.Count);
            Assert.Equal(typeof(AddRequestHeaderAction), actions.First().GetType());
            Assert.Equal(typeof(DeleteRequestHeaderAction), actions.Last().GetType());
            Assert.Equal(typeof(StatusCodeFilter), filter.GetType());
            Assert.Equal(4, filter.StatusCodes.Count);
            Assert.Contains(filter.StatusCodes, c => c == 200);
            Assert.Contains(filter.StatusCodes, c => c == 204);
            Assert.Contains(filter.StatusCodes, c => c == 301);
            Assert.Contains(filter.StatusCodes, c => c == 302);
        }

        [Fact]
        public void Reading_Should_Parse_Filter_Collection()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                filter: 
                  typeKind: FilterCollection        
                  operation: and
                  children:
                    - typeKind: JsonResponseFilter
                      inverted: true
                    - typeKind: ImageFilter  
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var _)!;

            var filter = (rule.Filter as FilterCollection)!;

            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.NotNull(rule.GetSingleAction());
            Assert.Equal(typeof(AddRequestHeaderAction), rule.GetSingleAction().GetType());
            Assert.Equal(typeof(FilterCollection), filter.GetType());
            Assert.Equal(2, filter.Children.Count);
            Assert.Equal(SelectorCollectionOperation.And, filter.Operation);

            Assert.True(filter.Children.First().Inverted);
            Assert.False(filter.Children.Last().Inverted);
        }

        [Theory]
        [InlineData(SelectorCollectionOperation.And)]
        [InlineData(SelectorCollectionOperation.Or)]
        public void Reading_Should_Parse_Enum(SelectorCollectionOperation operation)
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = $"""
                filter: 
                  typeKind: FilterCollection        
                  operation: {operation.ToString().ToLower()}  
                  children:
                    - typeKind: JsonResponseFilter
                      inverted: true
                    - typeKind: ImageFilter  
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var _)!;

            var filter = (rule.Filter as FilterCollection)!;

            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.NotNull(rule.GetSingleAction());
            Assert.Equal(typeof(AddRequestHeaderAction), rule.GetSingleAction().GetType());
            Assert.Equal(typeof(FilterCollection), filter.GetType());
            Assert.Equal(2, filter.Children.Count);
            Assert.Equal(operation, filter.Operation);

            Assert.True(filter.Children.First().Inverted);
            Assert.False(filter.Children.Last().Inverted);
        }

        [Fact]
        public void Reading_Should_Parse_Basic_Rule_Set()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: AddRequestHeaderAction
                      headerName: fluxzy
                      headerValue: on
                  - filter: 
                      typeKind: StatusCodeRedirectionFilter        
                    action : 
                      typeKind: ApplyCommentAction
                      comment: Go go go
                """;

            var rule = ruleConfigReader.TryGetRuleSetFromYaml(yamlContent, out _)!.Rules.First();

            var targetAction = (rule.GetSingleAction() as AddRequestHeaderAction)!;

            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.NotNull(rule.GetSingleAction());
            Assert.Equal(AnyFilter.Default.Identifier, rule.Filter.Identifier);
            Assert.Equal(typeof(AddRequestHeaderAction), rule.GetSingleAction().GetType());
            Assert.Equal("fluxzy", targetAction.HeaderName);
            Assert.Equal("on", targetAction.HeaderValue);
        }

        [Fact]
        public void Reading_Should_Fail_Basic_Rule_Set()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                ruldes:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: AddRequestHeaderAction
                      headerName: fluxzy
                      headerValue: on
                  - filter: 
                      typeKind: StatusCodeRedirectionFilter        
                    action : 
                      typeKind: ApplyCommentAction
                      comment: Go go go
                """;

            var rules = ruleConfigReader.TryGetRuleSetFromYaml(yamlContent, out var errorMessages)?.Rules;

            Assert.Null(rules);
            Assert.NotEmpty(errorMessages!);
        }

        [Fact]
        public void Reading_Should_Parse_Basic_Rule_Set_Multiple()
        {
            var ruleConfigReader = new RuleConfigParser();

            var yamlContent = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: AddRequestHeaderAction
                      headerName: fluxzy
                      headerValue: on
                  - filter: 
                      typeKind: StatusCodeRedirectionFilter        
                    action : 
                      typeKind: ApplyCommentAction
                      comment: Go go go
                """;

            var rules = ruleConfigReader.TryGetRuleSetFromYaml(yamlContent, out _)!.Rules;

            Assert.Equal(2, rules.Count);
        }

        [Theory]
        [MemberData(nameof(GetTestRules))]
        public void Writing_And_Reading_Rule_Should_Preserve_Object(Rule rule)
        {
            var parser = new RuleConfigParser();

            var yaml = parser.GetYamlFromRule(rule);

            var outputRule = parser.TryGetRuleFromYaml(yaml, out _)!;

            Assert.NotNull(outputRule);

            Assert.Equal(rule.Action, outputRule.GetSingleAction(), new GreedyActionComparer());
            Assert.Equal(rule.Filter, outputRule.Filter, new GreedyFilterComparer());
        }

        [Theory]
        [MemberData(nameof(GetTestRuleSet))]
        public void Writing_And_Reading_RuleSet_Should_Preserve_Object(RuleSet ruleSet)
        {
            var parser = new RuleConfigParser();

            var yaml = parser.GetYamlFromRuleSet(ruleSet);

            var outputRule = parser.TryGetRuleSetFromYaml(yaml, out _)!;

            Assert.NotNull(outputRule);
            Assert.Equal(ruleSet.Rules.Count, outputRule.Rules.Count);

            for (var index = 0; index < ruleSet.Rules.Count; index++)
            {
                var originalRule = ruleSet.Rules[index];
                var resultRule = outputRule.Rules[index];

                Assert.Equal(originalRule.GetSingleAction(), resultRule.GetSingleAction(), new GreedyActionComparer());
                Assert.Equal(originalRule.Filter, resultRule.Filter, new GreedyFilterComparer());
            }
        }

        public static IEnumerable<object[]> GetTestRules()
        {
            yield return new object[] {
                new Rule(
                    new ApplyCommentAction("Another comment"),
                    new AbsoluteUriFilter(".*", StringSelectorOperation.Regex)
                )
            };

            yield return new object[] {
                new Rule(
                    new ApplyTagAction {
                        Tag = new Tag(Guid.NewGuid(), "Random value")
                    },
                    new AnyFilter()
                )
            };

            yield return new object[] {
                new Rule(
                    new AddResponseHeaderAction("sdf", "sd"),
                    new FilterCollection(new HasCommentFilter(), new RequestHeaderFilter("Coco",
                        StringSelectorOperation.EndsWith, "Content-type"))
                )
            };

            yield return new object[] {
                new Rule(
#pragma warning disable CS0618 // Type or member is obsolete
                    new SetClientCertificateAction(new Certificate {
                        RetrieveMode = CertificateRetrieveMode.FromUserStoreSerialNumber,
                        Pkcs12File = "A pkcs12file",
                        Pkcs12Password = "A pkcs12 password",
                        SerialNumber = "absdf465"
                    }),
#pragma warning restore CS0618 // Type or member is obsolete
                    new IpEgressFilter(IPAddress.Loopback.ToString(), StringSelectorOperation.Contains)
                )
            };
        }

        public static IEnumerable<object[]> GetTestRuleSet()
        {
            yield return new object[] {
                new RuleSet(
                    new Rule(
                        new ApplyCommentAction("Another comment"),
                        new AbsoluteUriFilter(".*", StringSelectorOperation.Regex)
                    ),
                    new Rule(
                        new ApplyTagAction {
                            Tag = new Tag(Guid.NewGuid(), "Random value")
                        },
                        new AnyFilter()
                    )
                )
            };

            yield return new object[] {
                new RuleSet(
                    new Rule(
                        new AddResponseHeaderAction("sdf", "sd"),
                        new FilterCollection(new HasCommentFilter(), new RequestHeaderFilter("Coco",
                            StringSelectorOperation.EndsWith, "Content-type"))
                    ),
                    new Rule(
#pragma warning disable CS0618 // Type or member is obsolete
                        new SetClientCertificateAction(new Certificate {
                            RetrieveMode = CertificateRetrieveMode.FromUserStoreSerialNumber,
                            Pkcs12File = "A pkcs12file",
                            Pkcs12Password = "A pkcs12 password",
                            SerialNumber = "absdf465"
                        }),
#pragma warning restore CS0618 // Type or member is obsolete
                        new IpEgressFilter(IPAddress.Loopback.ToString(), StringSelectorOperation.Contains)
                    )
                )
            };
        }
    }

    internal static class RuleConfigurationExtensions
    {
        public static Action GetSingleAction(this RuleConfigContainer ruleConfigContainer)
        {
            var allActions = ruleConfigContainer.GetAllActions().ToList();
            Assert.Single(allActions);

            return allActions!.First();
        }
    }
}
