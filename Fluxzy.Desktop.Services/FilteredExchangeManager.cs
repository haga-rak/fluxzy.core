// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Readers;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services
{
    public class FilteredExchangeManager : ObservableProvider<FilteredExchangeState?>
    {
        private readonly ActiveViewFilterManager _activeViewFilterManager;

        protected override BehaviorSubject<FilteredExchangeState?> Subject { get; } = new(null);

        public FilteredExchangeManager(
            IObservable<FileState> fileStateObservable, IObservable<ViewFilter> viewFilterObservable,
            ActiveViewFilterManager activeViewFilterManager,
            IObservable<IArchiveReader> archiveReaderObservable,
            ForwardMessageManager forwardMessageManager)
        {
            _activeViewFilterManager = activeViewFilterManager;

            var trunkStateObservable = fileStateObservable.Select(fileState =>
                Observable.FromAsync(
                    async () => { return await fileState.ContentOperation.Observable.FirstAsync(); })
            ).Concat();

            trunkStateObservable.CombineLatest(
                                    viewFilterObservable, archiveReaderObservable,
                                    (trunkState, viewFilter, archiveReader) =>
                                    {
                                        // Ne pas s'abonner à trunk state ici 
                                        // viewFilter devra just s'appliquer au nouveau venu et devra sauvegarder son état 

                                        if (viewFilter.Empty)
                                            return null;

                                        var filteredIds =
                                            trunkState.Exchanges
                                                      .Where(e => viewFilter.Apply(null!, e.ExchangeInfo,
                                                          archiveReader))
                                                      .Select(e => e.Id);

                                        return new FilteredExchangeState(filteredIds);
                                    })
                                .DistinctUntilChanged()
                                .Do(v => Subject.OnNext(v))
                                .Do(v =>
                                {
                                    if (v != null)
                                        forwardMessageManager.Send(v);
                                })
                                .Subscribe();
        }

        public void OnExchangeAdded(ExchangeInfo exchange, IArchiveReader archiveReader)
        {
            var viewFilter = _activeViewFilterManager.Current;
            var filteredExchangeState = Subject.Value;

            if (filteredExchangeState != null)
            {
                var passFilter = viewFilter.Apply(null, exchange, archiveReader);

                if (passFilter)
                    filteredExchangeState.Exchanges.Add(exchange.Id);
            }
        }
    }
}
