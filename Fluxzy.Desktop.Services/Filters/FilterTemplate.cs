// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Filters
{
    public class FilterTemplate
    {
        public FilterTemplate(Filter filter)
        {
            Group = "General";

            if (filter.GetType().Namespace!.Contains("Request", StringComparison.OrdinalIgnoreCase))
            {
                Group = "Request filter";
            }

            if (filter.GetType().Namespace!.Contains("Response", StringComparison.OrdinalIgnoreCase))
            {
                Group = "Response filter";
            }

            Label = filter.FriendlyName;

            Filter = filter;
        }

        public string Label { get;  }

        public string Group { get; }

        public Filter Filter { get;  }


        public static implicit operator FilterTemplate(Filter filter) => new(filter);
    }
}