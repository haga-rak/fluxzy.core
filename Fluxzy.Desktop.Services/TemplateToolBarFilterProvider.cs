using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Desktop.Services
{
    public class TemplateToolBarFilterProvider : ObservableProvider<TemplateToolBarFilterModel>
    {
        private readonly HashSet<Guid> _defaultFilterSet;

        protected override BehaviorSubject<TemplateToolBarFilterModel> Subject { get; } =
            new(new TemplateToolBarFilterModel(new() , new()));
        
        public TemplateToolBarFilterProvider(ToolBarFilterProvider toolBarFilterProvider, 
            IObservable<DynamicStatistic> dynamicStatistic)
        {
            _defaultFilterSet = toolBarFilterProvider.GetDefault().Select(t => t.Filter.Identifier).ToHashSet();
            
            dynamicStatistic
                .Select(ts => ts.Agents.OrderBy(a => a.FriendlyName).Select(s => new AgentFilter(s)).OfType<Filter>().ToList())
                .Do(filters =>
                    Subject.OnNext(new TemplateToolBarFilterModel(Subject.Value.LastUsedFilters, filters)))
                .Subscribe(); 
        }

        public void SetNewFilter(Filter setFilter)
        {
            if (_defaultFilterSet.Contains(setFilter.Identifier))
                return;

            var lastUsedFilters = Subject.Value.LastUsedFilters;
            var agentFilters = Subject.Value.AgentFilters; 

            lastUsedFilters.RemoveAll(f => f.Identifier == setFilter.Identifier);

            lastUsedFilters.Insert(0, setFilter);

            while (lastUsedFilters.Count > 5)
                lastUsedFilters.RemoveAt(lastUsedFilters.Count - 1);

            Subject.OnNext(new TemplateToolBarFilterModel(lastUsedFilters, agentFilters));
        }
    }
}
