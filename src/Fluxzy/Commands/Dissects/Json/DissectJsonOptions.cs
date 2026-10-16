// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluxzy.Cli.Commands.Dissects.Json
{
    internal static class DissectJsonOptions
    {
        public static JsonSerializerOptions Indented { get; } = Create(true);

        public static JsonSerializerOptions Compact { get; } = Create(false);

        private static JsonSerializerOptions Create(bool indented)
        {
            return new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = indented,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
        }
    }
}
