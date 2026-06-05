// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fluxzy;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Yaml;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Action = Fluxzy.Rules.Action;

namespace Fluxzy.Tests.UnitTests.Rules
{
    /// <summary>
    ///     Round-trips every concrete action and filter through the reader so a type that fails surfaces.
    /// </summary>
    public class RuleParsingExhaustive
    {
        private static readonly RuleConfigParser Parser = new();

        private static IEnumerable<Type> ConcreteTypes(Type baseType)
        {
            return baseType.Assembly.GetTypes()
                           .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType))
                           .OrderBy(t => t.Name);
        }

        private static object ForceCreate(Type type)
        {
            return new RuleObjectFactory().Create(type);
        }

        [Fact]
        public void Every_Concrete_Filter_RoundTrips_To_Same_Type()
        {
            var failures = new List<string>();

            foreach (var type in ConcreteTypes(typeof(Filter))) {
                try {
                    var filter = (Filter) ForceCreate(type);
                    var yaml = Parser.GetYamlFromRule(new Rule(new ApplyCommentAction("comment"), filter));
                    var parsed = Parser.TryGetRuleFromYaml(yaml, out var errors);

                    if (errors is { Count: > 0 }) {
                        failures.Add($"{type.Name}: {string.Join("; ", errors.Select(e => e.Message))}");
                    }
                    else if (parsed?.Filter == null) {
                        failures.Add($"{type.Name}: parsed filter is null");
                    }
                    else if (parsed.Filter.GetType() != type) {
                        failures.Add($"{type.Name}: parsed as {parsed.Filter.GetType().Name}");
                    }
                }
                catch (Exception ex) {
                    failures.Add($"{type.Name}: threw {ex.GetType().Name}: {ex.Message}");
                }
            }

            Assert.True(failures.Count == 0, $"{failures.Count} filter(s) failed round-trip:\n{string.Join("\n", failures)}");
        }

        [Fact]
        public void Every_Concrete_Action_RoundTrips_To_Same_Type()
        {
            var failures = new List<string>();

            foreach (var type in ConcreteTypes(typeof(Action))) {
                try {
                    var action = (Action) ForceCreate(type);
                    var yaml = Parser.GetYamlFromRule(new Rule(action, new AnyFilter()));
                    var parsed = Parser.TryGetRuleFromYaml(yaml, out var errors);

                    if (errors is { Count: > 0 }) {
                        failures.Add($"{type.Name}: {string.Join("; ", errors.Select(e => e.Message))}");
                    }
                    else if (parsed == null || !parsed.GetAllActions().Any()) {
                        failures.Add($"{type.Name}: parsed action is null");
                    }
                    else if (parsed.GetAllActions().First().GetType() != type) {
                        failures.Add($"{type.Name}: parsed as {parsed.GetAllActions().First().GetType().Name}");
                    }
                }
                catch (Exception ex) {
                    failures.Add($"{type.Name}: threw {ex.GetType().Name}: {ex.Message}");
                }
            }

            Assert.True(failures.Count == 0, $"{failures.Count} action(s) failed round-trip:\n{string.Join("\n", failures)}");
        }

        [Fact]
        public void New_Reader_Matches_Previous_Reader_For_Every_Example()
        {
            // For the same YAML the new reader must produce the same object as the old System.Text.Json
            // bridge. Comparing re-serialized output isolates read divergences from shared serializer quirks.
            var failures = new List<string>();

            foreach (var type in ConcreteTypes(typeof(Filter))) {
                var instances = new List<Filter> { (Filter) ForceCreate(type) };
                instances.AddRange(ExamplesOf(() => ((Filter) ForceCreate(type)).GetExamples().Select(e => e.Filter), type, failures));

                foreach (var instance in instances) {
                    AssertReaderParity(Parser.GetYamlFromRule(new Rule(new ApplyCommentAction("comment"), instance)),
                        $"filter {type.Name}", failures);
                }
            }

            foreach (var type in ConcreteTypes(typeof(Action))) {
                var instances = new List<Action> { (Action) ForceCreate(type) };
                instances.AddRange(ExamplesOf(() => ((Action) ForceCreate(type)).GetExamples().Select(e => e.Action), type, failures));

                foreach (var instance in instances) {
                    AssertReaderParity(Parser.GetYamlFromRule(new Rule(instance, new AnyFilter())),
                        $"action {type.Name}", failures);
                }
            }

            Assert.True(failures.Count == 0, $"{failures.Count} case(s) diverged from the previous reader:\n{string.Join("\n", failures)}");
        }

        private static void AssertReaderParity(string yaml, string label, List<string> failures)
        {
            RuleConfigContainer? expected;

            try {
                expected = PreviousReader(yaml);
            }
            catch {
                // The old reader could not handle this type, so there is nothing to compare against.
                // The new reader is validated here by the type round-trip tests.
                return;
            }

            var actual = Parser.TryGetRuleFromYaml(yaml, out var errors);

            if (errors is { Count: > 0 } || actual == null) {
                failures.Add($"{label}: new reader failed: {string.Join("; ", errors?.Select(e => e.Message) ?? Enumerable.Empty<string>())}\n{yaml}");

                return;
            }

            var expectedYaml = Parser.GetYamlFromRule(new Rule(expected.GetSingleAction(), expected.Filter));
            var actualYaml = Parser.GetYamlFromRule(new Rule(actual.GetSingleAction(), actual.Filter));

            if (expectedYaml != actualYaml) {
                failures.Add($"{label}: read result differs from previous reader\n--- previous ---\n{expectedYaml}\n--- new ---\n{actualYaml}");
            }
        }

        /// <summary>
        ///     Reproduces the previous reader (YamlDotNet object graph bridged through System.Text.Json).
        /// </summary>
        private static RuleConfigContainer PreviousReader(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                               .WithNamingConvention(CamelCaseNamingConvention.Instance)
                               .Build();

            using var reader = new StringReader(yaml);
            var raw = deserializer.Deserialize(reader);

            var json = JsonSerializer.Serialize(raw, GlobalArchiveOption.ConfigSerializerOptions);

            return JsonSerializer.Deserialize<RuleConfigContainer>(json, GlobalArchiveOption.ConfigSerializerOptions)!;
        }

        private static IEnumerable<T> ExamplesOf<T>(Func<IEnumerable<T>> provider, Type type, List<string> failures)
        {
            try {
                return provider().ToList();
            }
            catch (Exception ex) {
                failures.Add($"{type.Name}: GetExamples threw {ex.GetType().Name}: {ex.Message}");

                return Enumerable.Empty<T>();
            }
        }
    }
}
