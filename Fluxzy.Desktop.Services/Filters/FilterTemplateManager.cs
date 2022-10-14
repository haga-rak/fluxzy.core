using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Desktop.Services.Filters
{
    public class FilterTemplateManager
    {
        public List<FilterTemplate> ReadAvailableTemplates()
        {
            var res = new List<FilterTemplate>()
            {
                new FullUrlFilter(string.Empty),
                new HostFilter(string.Empty),
            };

            return res; 
        }
    }

    public class FilterTemplate
    {
        public FilterTemplate(Filter filter)
        {
            Group = (filter.GetType().Namespace?.Contains("Request", StringComparison.OrdinalIgnoreCase)
                     ?? false)
                ? "Request filter"
                : "Response filter"; 

            Label = filter.FriendlyName;

            Filter = filter;
        }

        public string Label { get;  }

        public string Group { get; }

        public Filter Filter { get;  }


        public static implicit operator FilterTemplate(Filter filter) => new(filter);
    }

}
