// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Models
{
    public class ToolBarFilter
    {
        public string? ShortName { get; }

        public Filter Filter { get; }

        public string? Description { get; }

        public ToolBarFilter(Filter filter)
        {
            ShortName = filter.ShortName;
            Filter = filter;
            Description = filter.FriendlyName;
        }
    }
}
