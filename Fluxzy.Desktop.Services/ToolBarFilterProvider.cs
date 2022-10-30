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
            new ToolBarFilter(AnyFilter.Default),
            new ToolBarFilter(new ContentTypeJsonFilter()),
            new ToolBarFilter(new MethodFilter("POST")),
            new ToolBarFilter(new StatusCodeSuccessFilter()),
            new ToolBarFilter(new FilterCollection(
                    new StatusCodeClientErrorFilter(),
                    new StatusCodeServerErrorFilter()
                )
                {
                    Operation = SelectorCollectionOperation.Or,
                    ExplicitShortName = "err",
                    Description = "Error 4XX and 5XX",
                    Identifier = Guid.Parse("E4B4D0B9-44CC-453B-9B13-8B06F1008B89")
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
