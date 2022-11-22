// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization.TypeInspectors;

namespace Fluxzy.Rules
{
    internal class SortedTypeInspector : TypeInspectorSkeleton
    {
        private readonly ITypeInspector _innerTypeInspector;

        public SortedTypeInspector(ITypeInspector innerTypeInspector)
        {
            _innerTypeInspector = innerTypeInspector;
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            var properties = _innerTypeInspector.GetProperties(type, container);

            return properties.OrderByDescending(x => x.Name == "typeKind");
        }
    }

    public class RuleConfigParser
    {
        public string GetYamlFromRule(Rule rule)
        {
            var serializer = BuildDefaultSerializer();

            return serializer.Serialize(graph : rule);
        }

        public string GetYamlFromRuleSet(RuleSet ruleSet)
        {
            var serializer = BuildDefaultSerializer();
            return serializer.Serialize(graph : ruleSet);
        }

        public RuleSet? TryGetRuleSetFromYaml(string yamlContent, out List<RuleConfigReaderError>? readErrors)
        {
            var deserializer = BuildDefaultDeserializer();
            using var stringReader = new StringReader(yamlContent);
            var result = new RuleSet();

            Dictionary<string, object> rawObject;

            try
            {
                rawObject = deserializer.Deserialize<Dictionary<string, object>>(stringReader);

                if (rawObject.Any(r => r.Key != "rules"))
                {
                    readErrors = new List<RuleConfigReaderError>()
                    {
                        new($"Unknown entries found {string.Join(", ", rawObject.Where(r => r.Key != "rules").Select(s => s.Key))}. Expected \"rules\"")
                    };

                    return null;
                }
            }
            catch (Exception e)
            {
                if (e is SemanticErrorException see)
                {
                    readErrors = new List<RuleConfigReaderError>()
                    {
                        new($"{see.Message} {see.End}")
                    };

                    return null;
                }

                readErrors = new List<RuleConfigReaderError>()
                {
                    new($"Unable to read yaml file : {e.Message}")
                };

                return null;
            }

            readErrors = new List<RuleConfigReaderError>();

            int ruleIndex = 1;

            if (rawObject.TryGetValue("rules", out var tempList) && tempList is ICollection<object> items)
            {
                foreach (var item in items)
                {
                    var current = InternalTryGetRuleFromYaml(out var partialErrors, item);

                    var ruleName = current?.Name ?? $"Rule #{ruleIndex}";

                    ruleIndex++;

                    if (current == null)
                    {
                        readErrors.Add(new RuleConfigReaderError($"Error in “{ruleName}”"));
                        readErrors.AddRange(partialErrors);

                        continue; 
                    }

                    result.Rules.Add(current);
                }
            }
            
            return readErrors.Any() ? null : result; 
        }

        public Rule? TryGetRuleFromYaml(string yamlContent,
            out List<RuleConfigReaderError>? readErrors)
        {
            var deserializer = BuildDefaultDeserializer();

            using var stringReader = new StringReader(yamlContent);

            try
            {
                var rawObject = deserializer.Deserialize(stringReader);
                return InternalTryGetRuleFromYaml(out readErrors, rawObject);
            }
            catch (Exception ex)
            {
                if (ex is SemanticErrorException see)
                {
                    readErrors = new List<RuleConfigReaderError>()
                    {
                        new($"{see.Message} {see.End}")
                    };

                    return null;
                }

                readErrors = new List<RuleConfigReaderError>()
                {
                    new("Not a valid yaml " + ex.Message)
                };

                return null;  
            }
        }

        private static Rule? InternalTryGetRuleFromYaml(out List<RuleConfigReaderError> readErrors, object? rawObject)
        {
            var flatJson = JsonSerializer.Serialize(rawObject, GlobalArchiveOption.DefaultSerializerOptions);

            readErrors = new List<RuleConfigReaderError>();

            // TODO skip entirely System.Text.Json bridge 
            // Main downside of current method is the user is unable to determine in which line the error occurs
            // 
            Rule? rule;

            try
            {
                rule = JsonSerializer.Deserialize<Rule?>(flatJson, GlobalArchiveOption.DefaultSerializerOptions);
            }
            catch (Exception e)
            {
                readErrors.Add(new RuleConfigReaderError(e.Message));

                return null;
            }

            if (rule == null)
            {
                readErrors.Add(new RuleConfigReaderError($"Cannot parse rule"));

                return null;
            }

            if (rule.Filter == null!)
            {
                readErrors.Add(new RuleConfigReaderError($"Unable to detect filter matching this rule"));

                return null;
            }

            if (rule.Action == null!)
            {
                readErrors.Add(new RuleConfigReaderError($"Unable to detect action matching this rule"));

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

        public string Message { get;  }

        public override string ToString()
        {
            return Message;
        }


    }

    public class RuleSet
    {
        public RuleSet(params Rule[] rules)
        {
            Rules = rules.ToList();
        }

        public List<Rule> Rules { get; set; }
    }
}