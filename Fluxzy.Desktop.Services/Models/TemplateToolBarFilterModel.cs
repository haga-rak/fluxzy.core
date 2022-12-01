using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ViewOnlyFilters;

namespace Fluxzy.Desktop.Services.Models
{
    public class TemplateToolBarFilterModel
    {
        private static readonly List<Filter> DefaultQuickFilters = new List<Filter> {
            new SearchTextFilter("") {
                Description = "Find text", 
                SearchInResponseBody = true, 
                SearchInRequestBody = true
            },
            new PathFilter("", StringSelectorOperation.Contains) {
                Description = "Fitler by url"
            },
            new HostFilter("", StringSelectorOperation.Exact) {
                Description = "Filter by host"
            }
        };

        public TemplateToolBarFilterModel(List<Filter> lastUsedFilters, List<Filter> agentFilters)
        {
            LastUsedFilters = lastUsedFilters;
            AgentFilters = agentFilters;
        }

        public List<Filter> QuickFilters => DefaultQuickFilters;

        public List<Filter> LastUsedFilters { get; }

        public List<Filter> AgentFilters { get;  }
    }
}