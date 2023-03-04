// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Models
{
    public class ToolBarFilter
    {
        public ToolBarFilter(Filter filter)
        {
            ShortName = filter.ShortName;
            Filter = filter;
            Description = filter.FriendlyName;
        }

        public string? ShortName { get; }

        public Filter Filter { get; }

        public string? Description { get; }
    }
}
