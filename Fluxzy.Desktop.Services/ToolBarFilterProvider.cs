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
            new("json", new ContentTypeJsonFilter()),
            new("post", new MethodFilter("POST")),
        };

        public IReadOnlyCollection<ToolBarFilter> GetDefault()
        {
            return _defaults; 
        }
    }
}