using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Xunit;

namespace Fluxzy.Tests.Configurations
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

            var targetAction = (rule.Action as AddRequestHeaderAction)!;


            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.NotNull(rule.Action);
            Assert.Equal(AnyFilter.Default.Identifier, rule.Filter.Identifier);
            Assert.Equal(typeof(AddRequestHeaderAction), rule.Action.GetType());
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
            Assert.NotNull(rule.Action);
            Assert.Equal(typeof(AddRequestHeaderAction), rule.Action.GetType());
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
                    - typeKind: ContentTypeJsonFilter
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
            Assert.NotNull(rule.Action);
            Assert.Equal(typeof(AddRequestHeaderAction), rule.Action.GetType());
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
                  operation: { operation.ToString().ToLower()} 
                  children:
                    - typeKind: ContentTypeJsonFilter
                      inverted: true
                    - typeKind: ImageFilter  
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """ ;

            var rule = ruleConfigReader.TryGetRuleFromYaml(yamlContent, out var _)!;

            var filter = (rule.Filter as FilterCollection)!;

            Assert.NotNull(rule);
            Assert.NotNull(rule.Filter);
            Assert.NotNull(rule.Action);
            Assert.Equal(typeof(AddRequestHeaderAction), rule.Action.GetType());
            Assert.Equal(typeof(FilterCollection), filter.GetType());
            Assert.Equal(2, filter.Children.Count);
            Assert.Equal(operation, filter.Operation);

            Assert.True(filter.Children.First().Inverted);
            Assert.False(filter.Children.Last().Inverted);
        }

        [Theory]
        [MemberData(nameof(GetTestRules))]
        public void Writing_And_Reading_Should_Preserve_Object(Rule rule)
        {
            var parser = new RuleConfigParser();

            var yaml = parser.GetYamlFromRule(rule);

            var outputRule = parser.TryGetRuleFromYaml(yaml, out _)!;

            Assert.NotNull(outputRule);
            Assert.Equal(rule.Action, outputRule.Action, new GreedyActionComparer());
            Assert.Equal(rule.Filter, outputRule.Filter, new GreedyFilterComparer());
        }

        public static IEnumerable<object[]> GetTestRules()
        {
            yield return new object[] {
                new Rule(
                    new ApplyCommentAction("Another comment"),
                    new FullUrlFilter(".*", StringSelectorOperation.Regex)
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
                    new SetClientCertificateAction(new Certificate {
                        RetrieveMode = CertificateRetrieveMode.FromUserStoreSerialNumber,
                        Pkcs12File = "A pkcs12file",
                        Pkcs12Password = "A pkcs12 password",
                        SerialNumber = "absdf465"
                    }),
                    new IpEgressFilter(IPAddress.Loopback.ToString(), StringSelectorOperation.Contains)
                )
            };
        }
    }
}