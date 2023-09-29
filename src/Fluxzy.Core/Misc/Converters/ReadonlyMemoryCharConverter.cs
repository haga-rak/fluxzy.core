// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluxzy.Misc.Converters
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
            var byteCount = Encoding.UTF8.GetByteCount(value.Span);

            byte[]? allocated = null;

            try {
                var bufferedData = byteCount < 4096
                    ? stackalloc byte[byteCount]
                    : allocated = ArrayPool<byte>.Shared.Rent(byteCount);

                Encoding.UTF8.GetBytes(value.Span, bufferedData);
                writer.WriteStringValue(bufferedData);
            }
            finally {
                if (allocated != null)
                    ArrayPool<byte>.Shared.Return(allocated);
            }
        }
    }

    internal class BooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                return reader.GetBoolean();

            var stringchar = reader.GetString();

            if (bool.TryParse(stringchar, out var result))
                return result;

            throw new JsonException($"Cannot parse {stringchar} to boolean");
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
}
