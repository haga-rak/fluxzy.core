// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Readers;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Models
{
    public class ViewFilter
    {
        private static int _viewFilterId = 0; 

        private readonly Filter _effectiveFilter;

        public ViewFilter(Filter filter, Filter sourceFilter)
        {
            // unique id help to make distinct
            Id = Interlocked.Increment(ref _viewFilterId);

            Filter = filter;
            SourceFilter = sourceFilter;

            _effectiveFilter = Filter is AnyFilter && SourceFilter is AnyFilter
                ? AnyFilter.Default
                : new FilterCollection(Filter, SourceFilter) {
                    Operation = SelectorCollectionOperation.And
                };

        }

        public int Id { get; }

        public Filter Filter { get; }

        public Filter SourceFilter { get; }

        public bool Empty => _effectiveFilter is AnyFilter;

        public bool Apply(IAuthority authority, ExchangeInfo exchange, IArchiveReader archiveReader)
        {
            var filteringContext = new ExchangeInfoFilteringContext(archiveReader, exchange.Id);

            return _effectiveFilter.Apply(null, authority, exchange, filteringContext);
        }
    }
}
