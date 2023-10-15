// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluxzy.Misc.Converters
{
    internal class IpEndPointConverter : JsonConverter<IPEndPoint>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPEndPoint);
        }

        public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var document = JsonDocument.ParseValue(ref reader);

            var port = document.RootElement.GetProperty("port").GetInt32();
            var addressProperty = document.RootElement.GetProperty("address");

            var address = addressProperty.Deserialize<IPAddress>(options)!;

            return new IPEndPoint(address, port);
        }

        public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("address", value.Address.ToString());
            writer.WriteNumber("port", value.Port);

            writer.WriteEndObject();
        }
    }
}
