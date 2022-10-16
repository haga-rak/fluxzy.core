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
}