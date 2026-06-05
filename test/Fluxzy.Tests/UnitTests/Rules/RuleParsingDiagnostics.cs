// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using Fluxzy;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    /// <summary>
    ///     Covers the line and column carried by reader errors, and constructor-bound value binding.
    /// </summary>
    public class RuleParsingDiagnostics
    {
        private static readonly RuleConfigParser Parser = new();

        [Fact]
        public void Unknown_Filter_TypeKind_Reports_Name_And_Line()
        {
            var yaml = """
                filter:
                  typeKind: NoMoreFilter
                action:
                  typeKind: AddRequestHeaderAction
                  headerName: fluxzy
                  headerValue: on
                """;

            var rule = Parser.TryGetRuleFromYaml(yaml, out var errors);

            Assert.Null(rule);
            var error = Assert.Single(errors!);
            Assert.Contains("NoMoreFilter", error.Message);
            Assert.NotNull(error.Line);
            Assert.Contains("line ", error.Message);
        }

        [Fact]
        public void Invalid_Boolean_Reports_The_Offending_Line()
        {
            // 'inverted' carries a non-boolean value on line 3.
            var yaml = """
                filter:
                  typeKind: AnyFilter
                  inverted: notABoolean
                action:
                  typeKind: ApplyCommentAction
                  comment: hello
                """;

            var rule = Parser.TryGetRuleFromYaml(yaml, out var errors);

            Assert.Null(rule);
            var error = Assert.Single(errors!);
            Assert.Equal(3, error.Line);
            Assert.Contains("line 3", error.Message);
        }

        [Fact]
        public void Malformed_Yaml_Reports_A_Line()
        {
            var yaml = """
                filter:
                  typeKind: AnyFilter
                   headerName: misaligned
                action:
                  typeKind: ApplyCommentAction
                """;

            var rule = Parser.TryGetRuleFromYaml(yaml, out var errors);

            Assert.Null(rule);
            Assert.NotEmpty(errors!);
            Assert.All(errors!, e => Assert.NotNull(e.Line));
        }

        [Fact]
        public void RuleSet_Unknown_TypeKind_Reports_Name_And_Line()
        {
            var yaml = """
                rules:
                - filter:
                    typeKind: AnyFilter
                  action:
                    typeKind: NoSuchAction
                """;

            var ruleSet = Parser.TryGetRuleSetFromYaml(yaml, out var errors);

            Assert.Null(ruleSet);
            Assert.NotEmpty(errors!);
            Assert.Contains(errors!, e => e.Message.Contains("NoSuchAction") && e.Line.HasValue);
        }

        [Fact]
        public void Get_Only_Constructor_Bound_Filter_Property_Is_Preserved()
        {
            var yaml = Parser.GetYamlFromRule(new Rule(
                new ApplyCommentAction("c"),
                new AuthorityFilter(8080, "fluxzy.io", StringSelectorOperation.Exact)));

            var parsed = Parser.TryGetRuleFromYaml(yaml, out var errors);

            Assert.True(errors == null || errors.Count == 0);
            var filter = Assert.IsType<AuthorityFilter>(parsed!.Filter);
            Assert.Equal(8080, filter.Port);          // get-only, constructor-bound
            Assert.Equal("fluxzy.io", filter.Pattern);
        }

        [Fact]
        public void Get_Only_Constructor_Bound_Action_Property_Is_Preserved()
        {
            var yaml = Parser.GetYamlFromRule(new Rule(
                new ForwardAction("https://www.example.com"),
                new AnyFilter()));

            var parsed = Parser.TryGetRuleFromYaml(yaml, out var errors);

            Assert.True(errors == null || errors.Count == 0);
            var action = Assert.IsType<ForwardAction>(parsed!.GetSingleAction());
            Assert.Equal("https://www.example.com", action.Url);   // get-only, constructor-bound
        }

        [Fact]
        public void Immutable_Nested_Object_Is_Preserved()
        {
            var identifier = Guid.Parse("852D1563-5664-4F17-A4F2-BFE5F7C4993A");

            var yaml = Parser.GetYamlFromRule(new Rule(
                new ApplyTagAction { Tag = new Tag(identifier, "Hello fluxzy") },
                new AnyFilter()));

            var parsed = Parser.TryGetRuleFromYaml(yaml, out var errors);

            Assert.True(errors == null || errors.Count == 0);
            var action = Assert.IsType<ApplyTagAction>(parsed!.GetSingleAction());
            Assert.NotNull(action.Tag);
            Assert.Equal(identifier, action.Tag!.Identifier);   // get-only on immutable Tag
            Assert.Equal("Hello fluxzy", action.Tag.Value);
        }

        [Fact]
        public void Typoed_Root_Key_Is_Reported()
        {
            var yaml = """
                ruldes:
                - filter:
                    typeKind: AnyFilter
                  action:
                    typeKind: ApplyCommentAction
                    comment: hi
                """;

            var ruleSet = Parser.TryGetRuleSetFromYaml(yaml, out var errors);

            Assert.Null(ruleSet);
            Assert.NotEmpty(errors!);
        }
    }
}
