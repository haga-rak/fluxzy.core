// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
            var deserializer = BuildDefaultDeserializer();
            using var stringReader = new StringReader(yamlContent);

            Dictionary<string, object> rawObject;

            try {
                rawObject = deserializer.Deserialize<Dictionary<string, object>>(stringReader);

                if (rawObject.Any(r => r.Key != "rules")) {
                    readErrors = new List<RuleConfigReaderError> {
                        new(
                            $"Unknown entries found {string.Join(", ", rawObject.Where(r => r.Key != "rules").Select(s => s.Key))}. Expected \"rules\"")
                    };

                    return null;
                }
            }
            catch (Exception e) {
                if (e is SemanticErrorException see) {
                    readErrors = new List<RuleConfigReaderError> {
                        new($"{see.Message} {see.End}")
                    };

                    return null;
                }

                readErrors = new List<RuleConfigReaderError> {
                    new($"Unable to read yaml file : {e.Message}")
                };

                return null;
            }

            readErrors = new List<RuleConfigReaderError>();

            var ruleIndex = 1;

            var result = new RuleSet();

            if (rawObject.TryGetValue("rules", out var tempList) && tempList is ICollection<object> items) {
                foreach (var item in items) {
                    var current = InternalTryGetRuleFromYaml(out var partialErrors, item);

                    var ruleName = current?.Name ?? $"Rule #{ruleIndex}";

                    ruleIndex++;

                    if (current == null) {
                        readErrors.Add(new RuleConfigReaderError($"Error in “{ruleName}”"));
                        readErrors.AddRange(partialErrors);

                        continue;
                    }

                    result.Rules.Add(current);
                }
            }

            return readErrors.Any() ? null : result;
        }

        public RuleConfigContainer? TryGetRuleFromYaml(
            string yamlContent,
            out List<RuleConfigReaderError>? readErrors)
        {
            var deserializer = BuildDefaultDeserializer();

            using var stringReader = new StringReader(yamlContent);

            try {
                var rawObject = deserializer.Deserialize(stringReader);

                return InternalTryGetRuleFromYaml(out readErrors, rawObject);
            }
            catch (Exception ex) {
                if (ex is SemanticErrorException see) {
                    readErrors = new List<RuleConfigReaderError> {
                        new($"{see.Message} {see.End}")
                    };

                    return null;
                }

                readErrors = new List<RuleConfigReaderError> {
                    new("Not a valid yaml " + ex.Message)
                };

                return null;
            }
        }

        private static RuleConfigContainer? InternalTryGetRuleFromYaml(out List<RuleConfigReaderError> readErrors, object? rawObject)
        {
            var flatJson = JsonSerializer.Serialize(rawObject, GlobalArchiveOption.ConfigSerializerOptions);

            readErrors = new List<RuleConfigReaderError>();

            // TODO skip entirely System.Text.Json bridge 
            // Main downside of current method is the user is unable to determine in which line the error occurs
            // 
            RuleConfigContainer? rule;

            try {
                rule = JsonSerializer.Deserialize<RuleConfigContainer?>(flatJson, GlobalArchiveOption.ConfigSerializerOptions);
            }
            catch (Exception e) {
                readErrors.Add(new RuleConfigReaderError(e.Message));

                return null;
            }

            if (rule == null) {
                readErrors.Add(new RuleConfigReaderError("Cannot parse rule"));

                return null;
            }

            if (rule.Filter == null!) {
                readErrors.Add(new RuleConfigReaderError("Unable to detect filter matching this rule"));

                return null;
            }

            if (!rule.GetAllActions().Any()) {
                readErrors.Add(new RuleConfigReaderError("Unable to detect action matching this rule"));

                return null;
            }

            return rule;
        }

        private static IDeserializer BuildDefaultDeserializer()
        {
            var deserializer = new DeserializerBuilder()
                               .WithNamingConvention(CamelCaseNamingConvention.Instance)
                               .Build();

            return deserializer;
        }

        private static ISerializer BuildDefaultSerializer()
        {
            var serializer = new SerializerBuilder()
                             .WithNamingConvention(CamelCaseNamingConvention.Instance)
                             .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                             .WithTypeInspector(x => new SortedTypeInspector(x))
                             .Build();

            return serializer;
        }
    }

    public class RuleConfigReaderError
    {
        public RuleConfigReaderError(string message)
        {
            Message = message;
        }

        public string Message { get; }

        public override string ToString()
        {
            return Message;
        }
    }


    public class RuleSet
    {
        public RuleSet(params Rule[] rules)
        {
            Rules = RuleConfigContainer.CreateFrom(rules).ToList(); 
        }

        public List<RuleConfigContainer> Rules { get; set; }
    }
}
