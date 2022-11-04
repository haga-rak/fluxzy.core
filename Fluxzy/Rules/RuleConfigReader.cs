// // Copyright 2022 - Haga Rakotoharivelo
// 

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

            return JsonSerializer.Deserialize<Rule?>(flatJson, GlobalArchiveOption.JsonSerializerOptions);
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