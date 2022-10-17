using System.Collections.Immutable;
using System.Reactive.Subjects;
using System.Reflection.Metadata.Ecma335;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Desktop.Services
{
    public class TemplateToolBarFilterProvider : ObservableProvider<TemplateToolBarFilterModel>
    {
        protected override BehaviorSubject<TemplateToolBarFilterModel> Subject { get; } =
            new(
                new TemplateToolBarFilterModel(new List<Filter>()));

        public void SetNewFilter(Filter newFilter)
        {
            if (newFilter is AnyFilter)
                return; 

            var lastUsedFilters = Subject.Value.LastUsedFilters;

            lastUsedFilters.RemoveAll(f => f.Identifier == newFilter.Identifier); 
            
            lastUsedFilters.Insert(0, newFilter);

            while (lastUsedFilters.Count > 5) {
                lastUsedFilters.RemoveAt(lastUsedFilters.Count - 1);
            }

            Subject.OnNext(new TemplateToolBarFilterModel(lastUsedFilters));
        }
    }

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