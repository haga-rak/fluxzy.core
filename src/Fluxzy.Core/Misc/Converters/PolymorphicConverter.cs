// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluxzy.Misc.Converters
{
    public class PolymorphicConverter<T> : JsonConverter<T> where T : PolymorphicObject
    {
        private readonly Dictionary<string, Type> _typeMapping;

        public PolymorphicConverter(params Type[] args)
        {
            if (args.Any()) {
                _typeMapping = args.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

                return;
            }

            var foundTypes = typeof(T).Assembly.GetTypes()
                                      .Where(derivedType => typeof(T).IsAssignableFrom(derivedType)
                                                            && derivedType != typeof(T)
                                                            && !derivedType.IsAbstract
                                                            && derivedType.IsClass).ToList();

            _typeMapping = foundTypes.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(T);
        }

        private Type GetFinalType(ref Utf8JsonReader reader)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var typeKind = doc.RootElement.GetProperty("typeKind").GetString()!;

            // var autoSuffix 

            if (!_typeMapping.TryGetValue(typeKind, out var type)
                && !_typeMapping.TryGetValue(typeKind + typeof(T).Name, out type))
                throw new JsonException($"Cannot parse {typeKind} to a valid {typeof(T).Name}");

            return type;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeCalculatorReader = reader;

            var actualType = GetFinalType(ref typeCalculatorReader);

            var res = (T?) JsonSerializer.Deserialize(ref reader, actualType, options);

            return res;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
