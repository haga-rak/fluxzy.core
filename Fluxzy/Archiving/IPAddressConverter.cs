// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluxzy
{
    internal class IpAddressConverter : JsonConverter<IPAddress>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPAddress);
        }

        public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var strAddress = reader.GetString();
            return strAddress == null ? IPAddress.None : IPAddress.Parse(strAddress);
        }

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}