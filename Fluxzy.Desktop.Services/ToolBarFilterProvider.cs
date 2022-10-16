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
            new("all", AnyFilter.Default),
            new("json", new ContentTypeJsonFilter(), "Response that specify json content"),
            new("post", new MethodFilter("POST"), "POST method only"),
            new("err", new FilterCollection(
                    new StatusCodeClientErrorFilter(),
                    new StatusCodeServerErrorFilter()
                )
            {
                Operation = SelectorCollectionOperation.Or
            }, "Status code 4XX and 5XX"),

        };

        public IReadOnlyCollection<ToolBarFilter> GetDefault()
        {
            return _defaults; 
        }
    }
}