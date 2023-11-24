// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text.Json.Serialization;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using MessagePack;

namespace Fluxzy
{
    /// <summary>
    ///    Represent a header field
    /// </summary>
    [MessagePackObject]
    public class HeaderFieldInfo
    {
        protected bool Equals(HeaderFieldInfo other)
        {
            return Name.Span.Equals(other.Name.Span, StringComparison.OrdinalIgnoreCase) 
                   && Value.Span.Equals(other.Value.Span, StringComparison.Ordinal) 
                   && Forwarded == other.Forwarded;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != this.GetType())
                return false;

            return Equals((HeaderFieldInfo) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Value, Forwarded);
        }

        public HeaderFieldInfo(HeaderField original, bool doNotForwardConnectionHeader = false)
        {
            Name = original.Name;
            Value = original.Value;
            
            Forwarded = !Http11Constants.IsNonForwardableHeader(original.Name);

            if (doNotForwardConnectionHeader && Forwarded && Http11Constants.UnEditableHeaders.Contains(Name)) {
                Forwarded = false; 
            }
        }

        [JsonConstructor]
        [SerializationConstructor]
        public HeaderFieldInfo(ReadOnlyMemory<char> name, ReadOnlyMemory<char> value, bool forwarded)
        {
            Name = name;
            Value = value;
            Forwarded = forwarded;
        }

        public HeaderFieldInfo(string name, string value)
            : this(name.AsMemory(), value.AsMemory(), !Http11Constants.IsNonForwardableHeader(name))
        {

        }

        /// <summary>
        /// Header name
        /// </summary>
        [Key(0)]
        public ReadOnlyMemory<char> Name { get; set; }

        /// <summary>
        /// Header value
        /// </summary>
        [Key(1)]
        public ReadOnlyMemory<char> Value { get; set; }

        /// <summary>
        /// True if the header should be forwarded to the client or the server,
        /// otherwise the header will be ignored by fluxzy (typically for connection related headers)
        /// </summary>
        [Key(2)]
        public bool Forwarded { get; set; }

        /// <summary>
        /// Convenience method to convert this object to <see cref="HeaderField"/>
        /// </summary>
        /// <param name="d"></param>
        public static implicit operator HeaderFieldInfo(HeaderField d)
        {
            return new(d);
        }
    }
}
