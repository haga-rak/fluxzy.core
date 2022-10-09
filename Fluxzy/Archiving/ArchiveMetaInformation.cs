// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;

namespace Fluxzy
{
    public class ArchiveMetaInformation
    {
        public DateTime CaptureDate { get; set; } = DateTime.Now;

        public HashSet<Tag> Tags { get; set; } = new();

        public List<Filter> ViewFilters { get; set; } = new(); 
    }

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

    }
}