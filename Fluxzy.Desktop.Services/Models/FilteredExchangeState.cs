// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services.Models
{
    public class FilteredExchangeState : IEquatable<FilteredExchangeState>
    {
        public FilteredExchangeState(IEnumerable<int> exchanges)
        {
            Exchanges = new HashSet<int>(exchanges);
        }

        public HashSet<int> Exchanges { get; }

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

            return Equals((FilteredExchangeState) obj);
        }

        public override int GetHashCode()
        {
            return Exchanges.GetHashCode();
        }
    }
}
