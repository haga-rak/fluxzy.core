using System.Reactive.Subjects;
using Fluxzy.Rules.Filters;

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
}