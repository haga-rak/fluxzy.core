using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services
{
    public class TemplateToolBarFilterProvider : ObservableProvider<TemplateToolBarFilterModel>
    {
        private readonly HashSet<Guid> _defaultFilterSet;

        protected override BehaviorSubject<TemplateToolBarFilterModel> Subject { get; } =
            new(
                new TemplateToolBarFilterModel(new() , new()));

        public TemplateToolBarFilterProvider(ToolBarFilterProvider toolBarFilterProvider, IObservable<TrunkState> trunkState)
        {
            _defaultFilterSet = toolBarFilterProvider.GetDefault().Select(t => t.Filter.Identifier).ToHashSet();
        }

        public void SetNewFilter(Filter setFilter)
        {
            if (_defaultFilterSet.Contains(setFilter.Identifier))
                return;

            var lastUsedFilters = Subject.Value.LastUsedFilters;

            lastUsedFilters.RemoveAll(f => f.Identifier == setFilter.Identifier);

            lastUsedFilters.Insert(0, setFilter);

            while (lastUsedFilters.Count > 5)
                lastUsedFilters.RemoveAt(lastUsedFilters.Count - 1);

            Subject.OnNext(new TemplateToolBarFilterModel(lastUsedFilters));
        }
    }
}
