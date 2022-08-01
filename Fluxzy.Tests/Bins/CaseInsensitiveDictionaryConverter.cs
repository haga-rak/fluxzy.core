// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluxzy.Tests.Bins
{
    public sealed class CaseInsensitiveDictionaryConverter<TValue>
        : JsonConverter<Dictionary<string, TValue>>
    {
        public override Dictionary<string, TValue> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var dic = (Dictionary<string, TValue>) JsonSerializer.Deserialize(ref reader, typeToConvert, options)!;
            return new Dictionary<string, TValue>(dic, StringComparer.OrdinalIgnoreCase);
        }

        public override void Write(
            Utf8JsonWriter writer,
            Dictionary<string, TValue> value,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(
                writer, value, value.GetType(), options);
        }
    }
}