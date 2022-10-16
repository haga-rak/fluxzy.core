// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Models
{
    public class ToolBarFilter
    {
        public ToolBarFilter(string shortName, Filter filter)
        {
            ShortName = shortName;
            Filter = filter;
        }

        public string ShortName { get; }

        public Filter Filter { get;  }
    }
}