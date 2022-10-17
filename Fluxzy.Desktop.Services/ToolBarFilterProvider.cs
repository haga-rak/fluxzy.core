// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;

namespace Fluxzy.Desktop.Services
{
    public class ToolBarFilterProvider
    {
        private readonly List<ToolBarFilter> _defaults = new()
        {
            new(AnyFilter.Default),
            new(new ContentTypeJsonFilter()),
            new(new MethodFilter("POST")),
            new(new FilterCollection(
                    new StatusCodeClientErrorFilter(),
                    new StatusCodeServerErrorFilter()
                )
            {
                Operation = SelectorCollectionOperation.Or
            }),

        };

        public IReadOnlyCollection<ToolBarFilter> GetDefault()
        {
            return _defaults; 
        }
    }
}