// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Text.Json.Serialization;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy
{
    public class HeaderFieldInfo
    {
        [JsonConstructor]
        public HeaderFieldInfo()
        {

        }

        public HeaderFieldInfo(HeaderField original)
        {
            Name = original.Name;
            Value = original.Value;
            Forwarded = !Http11Constants.IsNonForwardableHeader(original.Name); 
        }

        public ReadOnlyMemory<char> Name { get; set; } 

        public ReadOnlyMemory<char> Value { get; set; } 
        
        public bool Forwarded { get; set; }

        public static implicit operator HeaderFieldInfo(HeaderField d) => new HeaderFieldInfo(d);
    }
}