// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fluxzy.Rules.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Fluxzy.Rules
{
    public class RuleConfigParser
    {
        public string GetYamlFromRule(Rule rule)
        {
            var serializer = BuildDefaultSerializer();
            return serializer.Serialize(rule);
        }

        public string GetYamlFromRuleSet(RuleSet ruleSet)
        {
            var serializer = BuildDefaultSerializer();

            return serializer.Serialize(ruleSet);
        }

        public RuleSet? TryGetRuleSetFromYaml(string yamlContent, out List<RuleConfigReaderError>? readErrors)
        {
            readErrors = new List<RuleConfigReaderError>();

            RuleSet? ruleSet;

            try {
                using var stringReader = new StringReader(yamlContent);
                ruleSet = RuleYamlDeserializerFactory.Instance.Deserialize<RuleSet>(stringReader);
            }
            catch (YamlException ex) {
                readErrors.Add(ToError(ex));

                return null;
            }
            catch (Exception ex) {
                readErrors.Add(new RuleConfigReaderError($"Unable to read yaml file : {ex.Message}"));

                return null;
            }

            if (ruleSet?.Rules == null || ruleSet.Rules.Count == 0) {
                readErrors.Add(new RuleConfigReaderError("No rule found. Expected a top-level \"rules\" entry."));

                return null;
            }

            var ruleIndex = 1;

            foreach (var container in ruleSet.Rules) {
                var ruleName = container.Name ?? $"Rule #{ruleIndex}";

                ValidateContainer(container, ruleName, $"rules[{ruleIndex - 1}]", readErrors);

                ruleIndex++;
            }

            return readErrors.Any() ? null : ruleSet;
        }

        public RuleConfigContainer? TryGetRuleFromYaml(
            string yamlContent,
            out List<RuleConfigReaderError>? readErrors)
        {
            readErrors = new List<RuleConfigReaderError>();

            RuleConfigContainer? rule;

            try {
                using var stringReader = new StringReader(yamlContent);
                rule = RuleYamlDeserializerFactory.Instance.Deserialize<RuleConfigContainer>(stringReader);
            }
            catch (YamlException ex) {
                readErrors.Add(ToError(ex));

                return null;
            }
            catch (Exception ex) {
                readErrors.Add(new RuleConfigReaderError("Not a valid yaml " + ex.Message));

                return null;
            }

            ValidateContainer(rule, "rule", path: null, readErrors);

            return readErrors.Any() ? null : rule;
        }

        private static void ValidateContainer(
            RuleConfigContainer? container, string ruleName, string? path,
            List<RuleConfigReaderError> readErrors)
        {
            if (container == null) {
                readErrors.Add(new RuleConfigReaderError("Cannot parse rule"));

                return;
            }

            if (container.Filter == null!) {
                readErrors.Add(new RuleConfigReaderError(
                    $"Unable to detect filter matching this rule (“{ruleName}”)", null, null, path));
            }

            if (!container.GetAllActions().Any()) {
                readErrors.Add(new RuleConfigReaderError(
                    $"Unable to detect action matching this rule (“{ruleName}”)", null, null, path));
            }
        }

        /// <summary>
        ///     Maps a <see cref="YamlException" /> to a reader error, using the innermost exception for the
        ///     most specific message and position.
        /// </summary>
        private static RuleConfigReaderError ToError(YamlException exception)
        {
            var innermost = exception;

            while (innermost.InnerException is YamlException inner) {
                innermost = inner;
            }

            var start = innermost.Start;

            return new RuleConfigReaderError(
                innermost.Message,
                start.Line > 0 ? (int) start.Line : (int?) null,
                start.Column > 0 ? (int) start.Column : (int?) null);
        }

        private static ISerializer BuildDefaultSerializer()
        {
            var serializer = new SerializerBuilder()
                             .WithNamingConvention(CamelCaseNamingConvention.Instance)
                             .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                             .WithTypeInspector(x => new SortedTypeInspector(x))
                             .WithTypeInspector(x => new IgnorePremadePropertiesFilter(x))
                             .Build();

            return serializer;
        }
    }
}
