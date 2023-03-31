// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;

namespace Fluxzy.Desktop.Services
{
    public class ToolBarFilterProvider
    {
        private readonly List<ToolBarFilter> _defaults = new() {
            new ToolBarFilter(AnyFilter.Default),
            new ToolBarFilter(new ContentTypeJsonFilter()),
            new ToolBarFilter(new MethodFilter("POST")),
            new ToolBarFilter(new StatusCodeSuccessFilter()),
            new ToolBarFilter(new FilterCollection(
                    new StatusCodeClientErrorFilter(),
                    new StatusCodeServerErrorFilter()
                ) {
                    Operation = SelectorCollectionOperation.Or,
                    ExplicitShortName = "err",
                    Description = "Error 4XX and 5XX"
                }
            ),
            new ToolBarFilter(new IsWebSocketFilter())
        };

        public IReadOnlyCollection<ToolBarFilter> GetDefault()
        {
            return _defaults;
        }
    }
}
