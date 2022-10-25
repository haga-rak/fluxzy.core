// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Concurrent;
using System.Collections.Immutable;
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
            new(new StatusCodeSuccessFilter()),
            new(new FilterCollection(
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
            new(new IsWebSocketFilter()),

        };

        public IReadOnlyCollection<ToolBarFilter> GetDefault()
        {
            return _defaults; 
        }
    }


}