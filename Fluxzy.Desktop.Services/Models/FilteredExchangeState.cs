// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class FilteredExchangeState : IEquatable<FilteredExchangeState>
    {
        public bool Equals(FilteredExchangeState? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Exchanges.SetEquals(other.Exchanges);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((FilteredExchangeState)obj);
        }

        public override int GetHashCode()
        {
            return Exchanges.GetHashCode();
        }

        public FilteredExchangeState(IEnumerable<int> exchanges)
        {
            Exchanges = new(exchanges);
        }

        public HashSet<int> Exchanges { get; }
    }
}