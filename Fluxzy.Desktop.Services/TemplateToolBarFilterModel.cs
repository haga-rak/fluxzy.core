using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Desktop.Services
{
    public class TemplateToolBarFilterModel
    {
        private static readonly List<Filter> DefaultQuickFilters = new List<Filter> {
            new PathFilter("", StringSelectorOperation.Contains) {
                Description = "Fitler by url"
            },
            new HostFilter("", StringSelectorOperation.Exact) {
                Description = "Filter by host"
            }
        };

        public TemplateToolBarFilterModel(List<Filter> lastUsedFilters)
        {
            LastUsedFilters = lastUsedFilters;
        }

        public List<Filter> QuickFilters => DefaultQuickFilters;

        public List<Filter> LastUsedFilters { get; }
    }
}