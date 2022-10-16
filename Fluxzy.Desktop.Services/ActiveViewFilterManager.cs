// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Hubs;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Filters;
using Microsoft.AspNetCore.SignalR;

namespace Fluxzy.Desktop.Services
{
    public class FilteredExchangeManager : ObservableProvider<FilteredExchangeState?>
    {
        protected override BehaviorSubject<FilteredExchangeState?> Subject => new (null);

        public FilteredExchangeManager(
            IObservable<TrunkState> trunkStateObservable, IObservable<ViewFilter> viewFilterObservable, 
            IHubContext<GlobalHub> hub)
        {

            viewFilterObservable.Do(
                (viewFilter) =>
                {

                }
            ).Subscribe();

            trunkStateObservable.CombineLatest(
                          viewFilterObservable,
                          (trunkState, viewFilter) =>
                          {
                              if (viewFilter.Filter is AnyFilter)
                                  return null;

                              var filteredIds =
                                  trunkState.Exchanges
                                            .Where(e => viewFilter.Filter.Apply(null, e.ExchangeInfo))
                                            .Select(e => e.Id);

                              return new FilteredExchangeState(filteredIds);
                          })
                    .DistinctUntilChanged()
                    .Do(v => Subject.OnNext(v))
                    .Do(v =>
                    {
                        hub.Clients.All.SendAsync(
                            "visibleExchangeUpdate", v);
                    })
                    // TODO alerter les clients de la mise à jour du filtre ? 
                    .Subscribe((c) =>
                    {
                        Console.WriteLine("next");

                    }, (c) =>
                    {
                        Console.WriteLine("error");

                    }, () =>
                    {
                        Console.WriteLine("Complete");
                    }); 
        }
    }


    public class ActiveViewFilterManager : ObservableProvider<ViewFilter>
    {
        protected override BehaviorSubject<ViewFilter> Subject { get; } = new(new ViewFilter(AnyFilter.Default));

        public void Update(ViewFilter filter)
        {
            Subject.OnNext(filter);
        }
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