// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Fluxzy.Rules.Yaml
{
    /// <summary>
    ///     Builds the deserializer that reads rule files into the typed model. Unmatched properties are
    ///     ignored (matching the previous reader, and the <c>typeKind</c> key which has no setter).
    /// </summary>
    internal static class RuleYamlDeserializerFactory
    {
        private static readonly IDeserializer Shared = Build();

        public static IDeserializer Instance => Shared;

        private static IDeserializer Build()
        {
            return new DeserializerBuilder()
                   .WithNamingConvention(CamelCaseNamingConvention.Instance)
                   .IgnoreUnmatchedProperties()
                   .WithObjectFactory(new RuleObjectFactory())
                   .WithNodeDeserializer(new RuleNodeDeserializer(), s => s.OnTop())
                   .Build();
        }
    }
}
