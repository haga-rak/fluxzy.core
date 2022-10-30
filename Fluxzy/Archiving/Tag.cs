// Copyright © 2022 Haga RAKOTOHARIVELO

using System;

namespace Fluxzy
{
    public class Tag : IEquatable<Tag>
    {
        public Tag(Guid identifier, string value)
        {
            Identifier = identifier;
            Value = value;
        }

        public Guid Identifier { get;   }

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
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((Tag)obj);
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
