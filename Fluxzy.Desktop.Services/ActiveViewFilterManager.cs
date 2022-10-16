// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Subjects;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services
{
    public class ActiveViewFilterManager : ObservableProvider<ViewFilter>
    {
        protected override BehaviorSubject<ViewFilter> Subject { get; } = new(new ViewFilter(AnyFilter.Default));

        public void Update(ViewFilter filter)
        {
            Subject.OnNext(filter);
        }

        public ViewFilter Current => Subject.Value;


    }

    public class ViewFilter
    {
        public ViewFilter(Filter filter)
        {
            Filter = filter;
        }

        public Filter Filter { get; }
    }

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
            if (obj.GetType() != this.GetType())
                return false;

            return Equals((FilteredExchangeState)obj);
        }

        public override int GetHashCode()
        {
            return Exchanges.GetHashCode();
        }

        public FilteredExchangeState(IEnumerable<int> exchanges)
        {
            Exchanges = new (exchanges);
        }

        public HashSet<int> Exchanges { get; }
    }
}