// Copyright © 2022 Haga Rakotoharivelo

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
            if (args.Any())
            {
                _typeMapping = args.ToDictionary(t => t.Name, t => t);
                return;
            }

            // if no explicit type load from assembly 

            var foundTypes = typeof(T).Assembly.GetTypes()
                                      .Where(derivedType => typeof(T).IsAssignableFrom(derivedType) 
                                                            && derivedType != typeof(T)
                                                            && !derivedType.IsAbstract
                                                            && derivedType.IsClass).ToList();

            _typeMapping = foundTypes.ToDictionary(t => t.Name, t => t);
        }

        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert == typeof(T);

        private Type GetFinalType(ref Utf8JsonReader reader)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var typeKind = doc.RootElement.GetProperty(nameof(PolymorphicObject.TypeKind)).GetString()!;

            if (!_typeMapping.TryGetValue(typeKind, out var type))
                throw new JsonException($"Cannot parse {typeKind}");

            return type;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeCalculatorReader = reader;

            var actualType = GetFinalType(ref typeCalculatorReader);

            return (T?)JsonSerializer.Deserialize(ref reader, actualType, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value, value.GetType());
    }
    
    public abstract class PolymorphicObject
    {
        public string TypeKind => GetType().Name;
    }
}