// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Models
{
    public class ToolBarFilter
    {
        public ToolBarFilter(string shortName, Filter filter, string? description = null)
        {
            ShortName = shortName;
            Filter = filter;
            Description = description;
        }

        public string ShortName { get; }

        public Filter Filter { get;  }

        public string?  Description { get; }
    }
}