// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Models
{
    public class ViewFilter
    {
        private readonly Filter _effectiveFilter;

        public ViewFilter(Filter filter, Filter sourceFilter)
        {
            Filter = filter;
            SourceFilter = sourceFilter;

            _effectiveFilter = Filter is AnyFilter && SourceFilter is AnyFilter
                ? AnyFilter.Default
                : new FilterCollection(Filter, SourceFilter)
                {
                    Operation = SelectorCollectionOperation.And
                };
        }

        public Filter Filter { get; }
        
        public Filter SourceFilter { get; }

        public bool Empty => _effectiveFilter is AnyFilter;

        public bool Apply(IAuthority authority, IExchange? exchange, IFilteringContext? filteringContext)
        {
            return _effectiveFilter.Apply(authority, exchange, filteringContext);
        }
    }
}