// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using MessagePack;
using System;

namespace Fluxzy
{
    [MessagePackObject]
    public class Tag : IEquatable<Tag>
    {
        public Tag(Guid identifier, string value)
        {
            Identifier = identifier;
            Value = value;
        }

        [Key(0)]
        public Guid Identifier { get; }

        [Key(1)]
        public string Value { get; }

        public bool Equals(Tag? other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Identifier.Equals(other.Identifier) && Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((Tag) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier, Value);
        }

        public override string ToString()
        {
            return $"{Value} ({Identifier})";
        }
    }
}
