// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluxzy
{
    internal class ReadonlyMemoryCharConverter : JsonConverter<ReadOnlyMemory<char>>
    {
        public override ReadOnlyMemory<char> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString().AsMemory();
        }

        public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<char> value, JsonSerializerOptions options)
        {
            var byteCount = System.Text.Encoding.UTF8.GetByteCount(value.Span);

            Span<byte> bufferedData = byteCount < 4096 ? 
                stackalloc byte[byteCount] : new byte[byteCount];

            System.Text.Encoding.UTF8.GetBytes(value.Span, bufferedData);

            writer.WriteStringValue(bufferedData);
        }
    }
}