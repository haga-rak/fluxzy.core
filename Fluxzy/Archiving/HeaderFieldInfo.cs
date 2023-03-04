// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text.Json.Serialization;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy
{
    public class HeaderFieldInfo
    {
        public HeaderFieldInfo(HeaderField original)
        {
            Name = original.Name;
            Value = original.Value;
            Forwarded = !Http11Constants.IsNonForwardableHeader(original.Name);
        }

        [JsonConstructor]
        public HeaderFieldInfo(ReadOnlyMemory<char> name, ReadOnlyMemory<char> value, bool forwarded)
        {
            Name = name;
            Value = value;
            Forwarded = forwarded;
        }

        public ReadOnlyMemory<char> Name { get; set; }

        public ReadOnlyMemory<char> Value { get; set; }

        public bool Forwarded { get; set; }

        public static implicit operator HeaderFieldInfo(HeaderField d)
        {
            return new(d);
        }
    }
}
