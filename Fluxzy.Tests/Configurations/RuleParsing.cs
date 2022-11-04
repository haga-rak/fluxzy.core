using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Xunit;

namespace Fluxzy.Tests.Configurations
{
    public class RuleParsing
    {
        [Fact]
        public void Should_Parse_Basic_Rule()
        {
            var ruleConfigReader = new RuleConfigReader();
            var yamlContent = """
                filter: 
                  typeKind: AnyFilter        
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRule(yamlContent, out var _)!;

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
        public void Should_Fail_Resolve_Filter_Invalid_Type_Kind()
        {
            var ruleConfigReader = new RuleConfigReader();
            var yamlContent = """
                filter: 
                  typeKind: NoMoreFilter        
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRule(yamlContent, out var errorMessages)!;
            
            Assert.Null(rule);
            Assert.NotEmpty(errorMessages!);
            Assert.Contains(errorMessages!, r => r.Message.Contains("NoMoreFilter"));
        }

        [Fact]
        public void Should_Fail_Resolve_Filter_No_Type_Kind()
        {
            var ruleConfigReader = new RuleConfigReader();
            var yamlContent = """
                filter:       
                action : 
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = ruleConfigReader.TryGetRule(yamlContent, out var errorMessages)!;
            
            Assert.Null(rule);
            Assert.NotEmpty(errorMessages!);
        }

        [Fact]
        public void Should_Parse_Extended_Type()
        {
            var ruleConfigReader = new RuleConfigReader();
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

            var rule = ruleConfigReader.TryGetRule(yamlContent, out var _)!;

            var targetAction = (rule.Action as AddRequestHeaderAction)!;
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

    }


}
