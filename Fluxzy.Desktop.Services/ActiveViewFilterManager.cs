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
        private readonly ActiveViewFilterManager _activeViewFilterManager;
        protected override BehaviorSubject<FilteredExchangeState?> Subject { get; } = new (null);

        public FilteredExchangeManager(
            IObservable<FileState> fileStateObservable, IObservable<ViewFilter> viewFilterObservable, 
            IHubContext<GlobalHub> hub, ActiveViewFilterManager activeViewFilterManager)
        {
            _activeViewFilterManager = activeViewFilterManager;

            var trunkStateObservable = fileStateObservable.Select(fileState =>
               System.Reactive.Linq.Observable.FromAsync(
                                  async () => await fileState.ContentOperation.Observable.FirstAsync())
                        
                ).Concat();

            trunkStateObservable.CombineLatest(
                                    viewFilterObservable,
                                    (fileState, viewFilter) =>
                                    {
                                        // Ne pas s'abonner à truk state ici 
                                        // viewFilter devra just s'appliquer au nouveau venu et devra sauvegarder son état 
                                        
                                        if (viewFilter.Filter is AnyFilter)
                                            return null;
                                        
                                        var filteredIds =
                                            fileState.Exchanges
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


        public void OnExchangeAdded(ExchangeInfo exchange)
        {
            var viewFilter = _activeViewFilterManager.Current;
            var filteredExchangeState = Subject.Value;

            if (filteredExchangeState != null)
            {
                var passFilter = viewFilter.Filter.Apply(null, exchange);

                if (passFilter)
                {
                    filteredExchangeState.Exchanges.Add(exchange.Id);
                }
            }
        }
    }


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