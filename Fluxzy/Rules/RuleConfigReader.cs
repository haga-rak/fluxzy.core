// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules
{
    public class RuleConfigReader
    {
        public Rule? TryGetRule(string yamlContent,
            out List<RuleConfigReaderError>? readErrors)
        {
            var deserializer = new DeserializerBuilder()
                               .WithNamingConvention(CamelCaseNamingConvention.Instance)
                               .Build();
            
            using var stringReader = new StringReader(yamlContent);

            var rawObject = deserializer.Deserialize(stringReader);

            var flatJson = JsonSerializer.Serialize(rawObject, GlobalArchiveOption.JsonSerializerOptions);

            readErrors = new List<RuleConfigReaderError>();

            // TODO skip entirely System.Text.Json bridge 
            // Main downside of current method is the user is unable to determine in which line the error occurs
            // 
            Rule? rule; 

            try {
               rule =  JsonSerializer.Deserialize<Rule?>(flatJson, GlobalArchiveOption.JsonSerializerOptions);
            }
            catch (Exception e) {
                readErrors.Add(new RuleConfigReaderError(e.Message));
                return null;
            }

            if (rule == null) {

                readErrors.Add(new RuleConfigReaderError($"Cannot parse rule"));
                return null; 
            }

            if (rule.Filter == null) {

                readErrors.Add(new RuleConfigReaderError($"Unable to detect filter matching this rule"));
                return null;
            }

            if (rule.Action == null) {

                readErrors.Add(new RuleConfigReaderError($"Unable to detect action matching this rule"));
                return null;
            }

            return rule; 
        }
    }


    public class RuleConfigReaderError
    {
        public RuleConfigReaderError(string message)
        {
            Message = message;
        }

        public string Message { get;  }
    }
}